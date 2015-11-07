using Q42.HueApi;
using Q42.HueApi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace HelloPhilipsHue
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private const string BRIDGE_IP = "192.168.1.225";
        private const string APP_ID = "1d0de40d14861c71a21c0c923d8a9e3";

        //command keys for queue
        private const string ON_OFF = "ON_OFF";
        private const string BRIGHTNESS = "BRIGHTNESS";
        private const string COLOR = "COLOR";

        private Dictionary<string, LightCommand> _commandQueue;
        DispatcherTimer _timer; //timer for queue

        ILocalHueClient _client;
        Light _light;
        List<string> _lightList;
        bool _isInitialized;
     

        public MainPage()
        {
            this.InitializeComponent();

            InitializeHue(); //fire and forget, don't wait on this call

            //initialize command queue
            _commandQueue = new Dictionary<string, LightCommand>();

            //intilialize command queue timer (avoid sending too many commands per second)
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(0.5);
            _timer.Tick += Timer_Tick;
        }

        public async void InitializeHue()
        {
            _isInitialized = false;
            //initialize client with bridge IP and app GUID
            _client = new LocalHueClient(BRIDGE_IP);
            _client.Initialize(APP_ID);

            //only working with light #1 in this demo
            _light = await _client.GetLightAsync("1");
            _lightList = new List<string>() { "1" };

            //initialize UI
            this.toggle_Power.IsOn = _light.State.On;
            string hexColor = _light.State.ToHex();
            byte brightness = _light.State.Brightness;
            this.slider_Brightness.Value = brightness;

            //there seems to be a defect with the color picker, the initial color isn't visually changing the control 
            //from code-behind. I'm leaving this code here in case it is fixed in the future.
            var rgb = HexToRGB(hexColor);
            this.color_Picker.SelectedColor = new SolidColorBrush(Color.FromArgb(brightness, rgb[0], rgb[1], rgb[2]));
           
            _isInitialized = true;
            _timer.Start();
        }

        private void Color_Picker_SelectedColorChanged(object sender, EventArgs e)
        {
            if (_isInitialized && this.toggle_Power.IsOn)
            {
                //Queue color change command
                LightCommand cmd = new LightCommand();
                var color = this.color_Picker.SelectedColor.Color;
                cmd.SetColor(color.R, color.G, color.B);
                cmd.Brightness = color.A;
                QueueCommand(COLOR, cmd);
            }
       
        }

        private void Toggle_Power_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isInitialized)
            {
                //queue power command
                LightCommand cmd = new LightCommand();
                cmd.On = this.toggle_Power.IsOn;
                QueueCommand(ON_OFF, cmd);
            }
        
        }

        private void Slider_Brightness_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (_isInitialized && toggle_Power.IsOn)
            {
                //queue brightness command
                LightCommand cmd = new LightCommand();
                cmd.Brightness = (byte)slider_Brightness.Value;
                QueueCommand(BRIGHTNESS, cmd);
            }

        }


        private void Timer_Tick(object sender, object e)
        {
            //stop timer
            _timer.Stop();

            //execute queue commands
            if (_commandQueue.Count > 0)
            {
                foreach (var cmd in _commandQueue)
                {
                    //fire and forget
                    _client.SendCommandAsync(cmd.Value, _lightList);
                }
            }

            //clear queue
            _commandQueue.Clear();

            //start timer back up again
            _timer.Start();
        }

        //helper method to queue light commands for execution
        private void QueueCommand(string commandType, LightCommand cmd)
        {
            if (_commandQueue.ContainsKey(commandType))
            {
                //replace with most recent
                _commandQueue[commandType] = cmd;
            }
            else
            {
                _commandQueue.Add(commandType, cmd);
            }

        }

        //helper method to convert hex to RGB
        private byte[] HexToRGB(string hex)
        {
            byte[] retvalue = new byte[3];
            if (hex.Contains('#'))
            {
                hex = hex.Remove(0, 1);
            }
           
            retvalue[0] = Convert.ToByte(hex.Substring(0, 2),16);
            retvalue[1] = Convert.ToByte(hex.Substring(2, 2),16);
            retvalue[2] = Convert.ToByte(hex.Substring(4, 2),16);

            return retvalue;

        }


    }
}
