﻿using HA4IoT.Contracts.Core;
using HA4IoT.Contracts.Services.System;
using HA4IoT.Hardware.RemoteSwitch;
using System.Threading.Tasks;
using HA4IoT.Controller.Dnf.Rooms;
using HA4IoT.Controller.Dnf.Enums;
using HA4IoT.Contracts;
using HA4IoT.Contracts.Hardware.I2C;
using HA4IoT.Hardware.I2C.I2CHardwareBridge;
using HA4IoT.Contracts.Hardware.Services;
using HA4IoT.Hardware.CCTools;
using System;
using HA4IoT.Hardware.Services;
using HA4IoT.Extensions;
using HA4IoT.Hardware.RemoteSwitch.Adapters;

namespace HA4IoT.Controller.Dnf
{
    internal class Configuration : IConfiguration
    {
        private const byte RASPBERRY_INTERRUPT = 4;

        private const byte ARDUINO_433_READ_PIN = 1;
        private const byte ARDUINO_433_SEND_PIN = 7;

        private const byte I2C_ADDRESS_ARDUINO = 50;
        private const byte I2C_ADDRESS_REL_1 = 32;    // GND - GND - GND (32)
        private const byte I2C_ADDRESS_REL_2 = 24;    // SCL - SCL - GND (24)
        private const byte I2C_ADDRESS_INPUT_1 = 88;  // SCL - SCL - SCL (88)
        private const byte I2C_ADDRESS_INPUT_2 = 16;  // GND - SCL - GND (16)

        private readonly InterruptMonitorService _interruptMonitorService;
        private readonly CCToolsDeviceService _ccToolsBoardService;
        private readonly IGpioService _gpioService;
        private readonly IDeviceRegistryService _deviceService;
        private readonly II2CBusService _i2CBusService;
        private readonly ISchedulerService _schedulerService;
        private readonly RemoteSocketService _remoteSocketService;
        private readonly IContainer _containerService;

        public Configuration(
            CCToolsDeviceService ccToolsBoardService,
            IGpioService gpioService,
            IDeviceRegistryService deviceService,
            II2CBusService i2CBusService,
            ISchedulerService schedulerService,
            RemoteSocketService remoteSocketService,
            InterruptMonitorService interruptMonitorService,
            IContainer containerService
            )
        {
            _interruptMonitorService = interruptMonitorService ?? throw new ArgumentNullException(nameof(interruptMonitorService));
            _ccToolsBoardService = ccToolsBoardService ?? throw new ArgumentNullException(nameof(ccToolsBoardService));
            _gpioService = gpioService ?? throw new ArgumentNullException(nameof(gpioService));
            _deviceService = deviceService ?? throw new ArgumentNullException(nameof(deviceService));
            _i2CBusService = i2CBusService ?? throw new ArgumentNullException(nameof(i2CBusService));
            _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
            _remoteSocketService = remoteSocketService ?? throw new ArgumentNullException(nameof(remoteSocketService));
            _containerService = containerService ?? throw new ArgumentNullException(nameof(containerService));
            
        }

        public Task ApplyAsync()
        {
            _interruptMonitorService.RegisterInterrupt("Default", _gpioService.GetInput(RASPBERRY_INTERRUPT));
            _interruptMonitorService.RegisterCallback("Default", _ccToolsBoardService.PollInputs);

            _ccToolsBoardService.RegisterDevice(CCToolsDevice.HSPE16_InputOnly, CCToolsDevices.HSPE16_88.ToString(), I2C_ADDRESS_INPUT_1);
            _ccToolsBoardService.RegisterDevice(CCToolsDevice.HSPE16_InputOnly, CCToolsDevices.HSPE16_16.ToString(), I2C_ADDRESS_INPUT_2);
            _ccToolsBoardService.RegisterDevice(CCToolsDevice.HSRel8, CCToolsDevices.HSRel8_32.ToString(), I2C_ADDRESS_REL_1);
            _ccToolsBoardService.RegisterDevice(CCToolsDevice.HSRel8, CCToolsDevices.HSRel8_24.ToString(), I2C_ADDRESS_REL_2);

            var i2CHardwareBridge = new I2CHardwareBridge(new I2CSlaveAddress(I2C_ADDRESS_ARDUINO), _i2CBusService, _schedulerService);
            _deviceService.RegisterDevice(i2CHardwareBridge);

            _remoteSocketService.Adapter = new I2CHardwareBridgeLdp433MhzBridgeAdapter(i2CHardwareBridge, ARDUINO_433_SEND_PIN);


            _containerService.GetInstance<LivingroomConfiguration>().Apply();
            _containerService.GetInstance<BalconyConfiguration>().Apply();
            _containerService.GetInstance<BedroomConfiguration>().Apply();
            _containerService.GetInstance<BathroomConfiguration>().Apply();
            _containerService.GetInstance<ToiletConfiguration>().Apply();
            _containerService.GetInstance<KitchenConfiguration>().Apply();
            _containerService.GetInstance<HallwayConfiguration>().Apply();
            _containerService.GetInstance<HouseConfiguration>().Apply();
            _containerService.GetInstance<StaircaseConfiguration>().Apply();

            return Task.FromResult(0);
        }


    }
}
