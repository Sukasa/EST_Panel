using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;

namespace EST_Panel
{

    public class ESTPanelSet
    {
        private SPI.Configuration _Config = null;
        SPI SPIBus;
        private byte[] _Buffer;
        private OutputPort _Latch;
        private int _Modules;
        private ESTPanel[] _Panels;

        public enum Lights
        {
            Alarm = 0,
            Trouble = 1,
            supervisory = 2
        }

        public ESTPanelSet(SPI.SPI_module SPIModule, Cpu.Pin ChipSelectPort, int NumModules, Cpu.Pin LatchPort)
        {
            _Config = new SPI.Configuration(ChipSelectPort, false, 0, 0, false, false, 5000, SPIModule);
            _Latch = new OutputPort(LatchPort, false);
            _Buffer = new byte[NumModules * 17];
            _Modules = NumModules;
            _Panels = new ESTPanel[_Modules];

            for (int Panel = 0; Panel < _Modules; Panel++)
                _Panels[Panel] = new ESTPanel(this, Panel);

            SPIBus = new SPI(_Config);
        }

        public ESTPanel[] Panels
        {
            get
            {
                return _Panels;
            }
        }

        public void Refresh()
        {
            int Count = 17 * _Modules;
            SPIBus.WriteRead(_Buffer, 0, Count, _Buffer, 12, Count - 12, 0);
            _Latch.Write(true);
            _Latch.Write(false);
        }

        public class ESTPanel
        {
            private int _IndexStart;
            private ESTPanelSet _Master;


            internal ESTPanel(ESTPanelSet SetMaster, int PanelIndex)
            {
                _Master = SetMaster;
                _IndexStart = PanelIndex;
            }

            public void SetLight(int ButtonIndex, Lights Light, bool LightState)
            {
                int BitAddr = (ButtonIndex * 3 + (int)Light);
                int LightValue = _Master._Buffer[(_IndexStart * 17) + BitAddr >> 3];

                if (LightState)
                    LightValue |= (1 << (BitAddr % 8));
                else
                    LightValue &= ~(1 << (BitAddr % 8));

                _Master._Buffer[(_IndexStart * 17) + BitAddr >> 3] = (byte)LightValue;
            }

            public bool ReadButton(int ButtonIndex)
            {
                return (_Master._Buffer[_IndexStart * 29 + (ButtonIndex >> 3)] & (1 << (ButtonIndex % 8))) != 0;
            }
        }

    }



    public class Program
    {
        public static void Main()
        {
            Random R = new Random();

            ESTPanelSet Set = new ESTPanelSet(SPI_Devices.SPI1, Pins.GPIO_PIN_D0, 1, Pins.GPIO_PIN_D1);
            ESTPanelSet.ESTPanel Panel = Set.Panels[0];

            while (true)
            {
                Panel.SetLight(R.Next(36), ESTPanelSet.Lights.Alarm, R.Next(2) == 1);
                Set.Refresh();
            }

        }

    }
}
