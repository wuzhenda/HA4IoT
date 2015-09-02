﻿using System;
using CK.HomeAutomation.Core;

namespace CK.HomeAutomation.Hardware.Drivers
{
    public class TemperatureAndHumiditySensorBridgeDriver
    {
        private readonly float[] _temperatures = new float[10];
        private readonly float[] _humidities = new float[10];

        private readonly int _bridgeAddress;
        private readonly I2CBus _i2CBus;

        public TemperatureAndHumiditySensorBridgeDriver(int address, HomeAutomationTimer timer, I2CBus i2CBus)
        {
            if (i2CBus == null) throw new ArgumentNullException(nameof(i2CBus));
            
            _bridgeAddress = address;
            _i2CBus = i2CBus;

            timer.Every(TimeSpan.FromSeconds(10)).Do(FetchValues);
        }

        public event EventHandler ValuesUpdated;

        public float GetTemperature(int sensorId)
        {
            return _temperatures[sensorId];
        }

        public float GetHumidity(int sensorId)
        {
            return _humidities[sensorId];
        }

        private void FetchValues()
        {
            for (int i = 0; i < 10; i++)
            {
                FetchValues(i);    
            }
            
            ValuesUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void FetchValues(int id)
        {
            byte[] writeBuffer = { (byte)id };
            byte[] readBuffer = new byte[8];

            // TODO: Repeatet start conditions are not(!) working with the Pi2 and the Arduino Nano (maybe the Arduino Wire library hack is the problem here).
            // The Arduino code is currently running without the hack and thus, a separeted write and read is required.

            ////_i2CBus.Execute(_bridgeAddress, bus => bus.WriteRead(writeBuffer, readBuffer));
            _i2CBus.Execute(_bridgeAddress, bus =>
            {
                bus.Write(writeBuffer);
                bus.Read(readBuffer);
            }, false);

            _temperatures[id] = BitConverter.ToSingle(readBuffer, 0);
            _humidities[id] = BitConverter.ToSingle(readBuffer, 4);
        }
    }
}