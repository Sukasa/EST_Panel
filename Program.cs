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
        private byte[] _OutBuff;
        public byte[] _InBuff;
        private OutputPort _Latch;
        private int _Modules;
        private ESTPanel[] _Panels;

        public enum Lights
        {
            Alarm = 0,
            Trouble = 1,
            Supervisory = 2
        }

        public ESTPanelSet(SPI.SPI_module SPIModule, Cpu.Pin ChipSelectPort, int NumModules, Cpu.Pin LatchPort)
        {
            _Config = new SPI.Configuration(ChipSelectPort, false, 0, 0, false, false, 10000, SPIModule);
            _Latch = new OutputPort(LatchPort, false);
            _Buffer = new byte[(NumModules * 17)];
            _InBuff = new byte[17];
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
            for (int Panel = 0; Panel < _Modules; Panel++)
            {
                SPIBus.WriteRead(_Buffer, (Panel * 17), 17, _InBuff, 0, 17, 0); //Exchange IO Data with next panel
                Array.Copy(_InBuff, 0, _Buffer, Panel * 17, 5); // Copy button data in to buffer
            }
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
                int BitAddr = ((ButtonIndex * 3) + (int)Light);
                SetRaw(BitAddr, LightState);
            }

            public void SetRaw(int Index, bool LightState)
            {
                int ArrayIndex = (_IndexStart * 17) + 5 + (Index >> 3);
                int ModifyBit = 1 << (Index & 7);
                int LightValue = _Master._Buffer[ArrayIndex];

                if (LightState)
                    LightValue |= ModifyBit;
                else
                    LightValue &= ~ModifyBit;

                _Master._Buffer[ArrayIndex] = (byte)LightValue;
            }

            public bool ReadButton(int ButtonIndex)
            {
                return (_Master._Buffer[(_IndexStart * 17) + (ButtonIndex >> 3)] & (1 << (ButtonIndex & 7))) == 0;
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
