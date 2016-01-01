using System;
using System.Threading;
using Microsoft.SPOT;
using System.Text;

namespace MastersThesisCountDown.I2C
{
    public class KanaLCD : I2C
    {
        private enum RSbit : byte
        {
            Command = 0x00,
            TextContinue = 0xC0,
            TextEnd = 0x40
        }

        /// <summary>
        /// �J�iLCD�̃R�}���h�R�[�h
        /// </summary>
        private enum Command : byte
        {
            /// <summary>�������Ȃ�: Busy ��Ԃ��m�F����ׂ̃R�}���h (NUL)</summary>
            Null = 0x00,
            /// <summary>������ (SOH)</summary>
            Initialize = 0x01,
            /// <summary>�e�L�X�g�]�� (STX)</summary>
            SendText = 0x02,
            /// <summary>��ʃN���A (FF)</summary>
            ClearScreen = 0x0C,
            /// <summary>Backlight ON  (DC2)</summary>
            BackLightOn = 0x12,
            /// <summary>Backlight OFF (DC4)</summary>
            BackLightOff = 0x14,
            /// <summary>�G���R�[�h�ݒ� (0x30�`0x35)</summary>
            EncodingSetting = 0x30,
            /// <summary>�J�[�\���ړ�:�E (CUF)</summary>
            MoveRight = 0x43,
            /// <summary>�J�[�\���ړ�:�� (CUB)</summary>
            MoveLeft = 0x44,
            /// <summary>�J�[�\���ʒu y(0�`3), x(0�`39) (CUP) ���s(row)�w�肪��B��(col)�w��͌�B�ȗ��������� 0 �w�肵�����Ƃ݂Ȃ��B</summary>
            Locate = 0x48,
            /// <summary>�_�� OFF�F�f�t�H���g</summary>
            BlinkOff = 0x70,
            /// <summary>�_�� ON</summary>
            BlinkOn = 0x71,
            /// <summary>�J�[�\���\�� OFF�F�f�t�H���g</summary>
            CursorOff = 0x72,
            /// <summary>�J�[�\���\�� ON</summary>
            CursorOn = 0x73,
            /// <summary>��ʕ\�� OFF</summary>
            DisplayOff = 0x74,
            /// <summary>��ʕ\�� ON�F�f�t�H���g</summary>
            DisplayOn = 0x75,
            /// <summary>�����X�N���[�� OFF�F�f�t�H���g</summary>
            AutoScrollOff = 0x76,
            /// <summary>�����X�N���[�� ON</summary>
            AutoScrollOn = 0x77,
            /// <summary>������E�փJ�[�\�����i�ށF�f�t�H���g</summary>
            LeftToRight = 0x78,
            /// <summary>�E���獶�փJ�[�\�����i��</summary>
            RightToLeft = 0x79,
            /// <summary>�J�[�\���y�щ�ʃX�N���[�����z�[���ʒu�֖߂�</summary>
            RetHome = 0x7A,
            /// <summary>���[�U�[��`�����t�H���g��ݒ�</summary>
            SetUserFont = 0x7B,
            /// <summary>��ʃX�N���[��:�� (SL)</summary>
            DisplayScrollLeft = 0xC0,
            /// <summary>��ʃX�N���[��:�E (SR)</summary>
            DisplayScrollRight = 0xC1,
            /// <summary>�C�ӂ̖��ߗ�  : RS=0</summary>
            InstructionStream = 0xFE,
            /// <summary>�C�ӂ̃f�[�^��: RS=1</summary>
            DataStream = 0xFF,
        }

        /// <summary>
        /// �J�iLCD�̃X�e�[�^�X�R�[�h
        /// </summary>
        private enum Status
        {
            /// <summary>����</summary>
            Normal,
            /// <summary>����������Ă��Ȃ�</summary>
            NotInitialized
        }

        /// <summary>
        /// �J�iLCD�̉�ʃ��[�h
        /// ���ӁF���̉�ʃT�C�Y�́A�����܂œ�����DDRAM�������̐ݒ�ł���A�t����ʂɕ\���������ۂ̉�ʃT�C�Y�Ƃ͈���Ă���B
        ///       �Ⴆ�΁ASD1602H �Ȃ�ADDRAM�������I�ɂ� 40���~2�s���邪�A���ۂɕ\�������̂́A40���~2�s�̓���16���~2�s�����ł���A
        ///       �c���24���~2�s�̓��e���������̂Ȃ�AscrollDisplayLeft/Right �֐��ō��E�ɃX�N���[��������K�v������B
        /// </summary>
        public enum ScreenMode : byte
        {
            /// <summary>80���~1�s, �t�H���g5x8,  �g�p����DDRAM�A�h���X�F0x00�`0x4F  ���{�V�[���h���ʂ� SJ2 ���V���[�g�ASJ1 ���I�[�v���̎��́A�����I�ɂ��̃��[�h�ɂȂ�</summary>
            Screen80x1 = 0x00,
            /// <summary>80���~1�s, �t�H���g5x10, �g�p����DDRAM�A�h���X�F0x00�`0x4F  ���{�V�[���h���ʂ� SJ2 ���V���[�g�ASJ1 ���V���[�g�̎��́A�����I�ɂ��̃��[�h�ɂȂ�</summary>
            Screen80x1L = 0x10,
            /// <summary>40���~2�s, �t�H���g5x8,  �g�p����DDRAM�A�h���X�F0x00�`0x27/0x40�`0x67</summary>
            Screen40x2 = 0x20,
            /// <summary>8���~4�s, �t�H���g5x8,  �g�p����DDRAM�A�h���X�F0x00�`0x07/0x40�`0x47/0x08�`0x0F/0x48�`0x4F</summary>
            Screen8x4 = 0x30,
            /// <summary>10���~4�s, �t�H���g5x8,  �g�p����DDRAM�A�h���X�F0x00�`0x09/0x40�`0x49/0x0A�`0x13/0x4A�`0x53</summary>
            Screen10x4 = 0x40,
            /// <summary>16���~4�s, �t�H���g5x8,  �g�p����DDRAM�A�h���X�F0x00�`0x0F/0x40�`0x4F/0x10�`0x1F/0x50�`0x5F</summary>
            Screen16x4 = 0x50,
            /// <summary>20���~4�s, �t�H���g5x8,  �g�p����DDRAM�A�h���X�F0x00�`0x13/0x40�`0x53/0x14�`0x27/0x54�`0x67</summary>
            Screen20x4 = 0x60,
            /// <summary>40���~4�s, �t�H���g5x8,  �g�p����DDRAM�A�h���X�F0x00�`0x27/0x40�`0x67/0x00�`0x27/0x40�`0x67  ���{�V�[���h�ł͔�Ή�</summary>
            Screen40x4 = 0x70
        }

        public enum FlowDirection
        {
            LeftToRight,
            RightToLeft
        }

        private const int LcdCommandMax = 128;

        private int rowCount;
        private int columnCount;

        public KanaLCD(ushort address, int rowCount, int columnCount) : base(address, 100, 500)
        {
            Initialize(rowCount, columnCount);
        }


        public void Initialize(int rowCount, int columnCount, int waitTime = 80)
        {
            var screenMode = GetScreenMode(rowCount, columnCount);
            this.columnCount = columnCount;
            this.rowCount = rowCount;

            Thread.Sleep(waitTime);

            SendCommand((byte)Command.Initialize, (byte)screenMode);
        }

        public void ClearScreen()
        {
            SendCommand(Command.ClearScreen);
        }

        public void RetHome()
        {
            SendCommand(Command.RetHome);
        }

        public void CreateCharacter(byte index, byte[] fontData)
        {
            if (index >= 8)
            {
                throw new ArgumentOutOfRangeException("�C���f�b�N�X�̒l���s���ł��B�C���f�b�N�X��0�`7�͈̔͂ł���K�v������܂��B");
            }

            if (fontData.Length != 8)
            {
                throw new ArgumentException("�t�H���g�f�[�^�͗v�f��8��byte�^�z��ł���K�v������܂�");
            }

            Write((byte)Command.SetUserFont, index, fontData[7], fontData[6], fontData[5], fontData[4], fontData[3], fontData[2], fontData[1], fontData[0]);
            Thread.Sleep(2);
        }

        public void SetCursor(byte row, byte column)
        {
            if (column >= columnCount || row >= rowCount)
            {
                throw new ArgumentOutOfRangeException($"�s�E��̎w�肪�s���ł��B�s��0�`{rowCount}�A���0�`{columnCount}�̊ԂłȂ���΂Ȃ�܂���B");
            }

            Write((byte)Command.Locate, row, column);
            Thread.Sleep(2);
        }

        public void Write(char character)
        {
            Write(character.ToString());
        }

        public void Write(string message)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(message);
            var sentBytes = 0;

            do
            {
                var messageLength = byteArray.Length - sentBytes;
                // 1��̑��M�ōő�127�o�C�g�܂łɐ���
                if (messageLength > (LcdCommandMax - 1))
                {
                    messageLength = LcdCommandMax - 1;
                }

                // ���b�Z�[�W+�R�}���h1byte
                var buffer = new byte[messageLength + 1];
                buffer[0] = (byte)Command.SendText;
                for (int i = 0; i < messageLength; i++)
                {
                    buffer[i + 1] = byteArray[sentBytes + i];
                }

                Write(buffer);
                sentBytes += messageLength;
                Thread.Sleep(2);
            } while (byteArray.Length == sentBytes);
        }


        public void ScrollLeft(int scrollCount = 1)
        {
            for (int i = 0; i < scrollCount; i++)
            {
                SendCommand(Command.DisplayScrollLeft);
            }
        }

        public void ScrollRight(int scrollCount = 1)
        {
            for (int i = 0; i < scrollCount; i++)
            {
                SendCommand(Command.DisplayScrollRight);
            }
        }

        public void MoveCursorToLeft(int moveCount = 1)
        {
            for (int i = 0; i < moveCount; i++)
            {
                SendCommand(Command.MoveLeft);
            }
        }

        public void MoveCursorToRight(int moveCount = 1)
        {
            for (int i = 0; i < moveCount; i++)
            {
                SendCommand(Command.MoveRight);
            }
        }

        public byte CursorColumn
        {
            get
            {
                return (byte)(GetStatus()[0] & 0x7F);
            }
            set
            {
                SetCursor(CursorRow, value);
            }
        }

        public byte CursorRow
        {
            get
            {
                return (byte)(GetStatus()[1] & 0x03);
            }
            set
            {
                SetCursor(value, CursorColumn);
            }
        }

        public bool BlinksCursor
        {
            get
            {
                return ((GetStatus()[2] >> 4) & 0x01) != 0;
            }
            set
            {
                SendCommand(value ? Command.BlinkOn : Command.BlinkOff);
            }
        }

        public bool ShowsCursor
        {
            get
            {
                return ((GetStatus()[2] >> 5) & 0x01) != 0;
            }
            set
            {
                SendCommand(value ? Command.CursorOn : Command.CursorOff);
            }
        }

        public bool ShowsDisplay
        {
            get
            {
                return ((GetStatus()[2] >> 6) & 0x01) != 0;
            }
            set
            {
                SendCommand(value ? Command.DisplayOn : Command.DisplayOff);
            }
        }

        public bool AutoScroll
        {
            get
            {
                return ((GetStatus()[1] >> 2) & 0x01) != 0;
            }
            set
            {
                SendCommand(value ? Command.AutoScrollOn : Command.AutoScrollOff);
            }
        }

        public bool BackLight
        {
            get
            {
                return ((GetStatus()[3] >> 5) & 0x01) != 0;
            }
            set
            {
                SendCommand(value ? Command.BackLightOn : Command.BackLightOff);
            }
        }

        public FlowDirection CursorDirection
        {
            get
            {
                return (((GetStatus()[1] >> 3) & 0x01) != 0) ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
            }
            set
            {
                SendCommand(value == FlowDirection.LeftToRight ? Command.LeftToRight : Command.RightToLeft);
            }
        }

        public ScreenMode LcdScreenMode
        {
            get
            {
                return (ScreenMode)(GetStatus()[1] & 0x70);
            }
        }

        public bool LeftSwitchIsPressed => ((GetStatus()[3] >> 3) & 0x01) != 0;

        public bool RightSwitchIsPressed => ((GetStatus()[3] >> 4) & 0x01) != 0;

        private void SendCommand(params byte[] command)
        {
            Write(command);
            Thread.Sleep(2);
        }

        private void SendCommand(Command command)
        {
            SendCommand((byte)command);
        }

        private byte[] GetStatus()
        {
            var data = Read(4);
            var status = new byte[4];

            // �ǂݎ�����S�� data �̒��ŁAbit7 �� 1 �ɂȂ��Ă��� data ���A�J�n status �ł�
            if ((data[0] & 0x80) != 0)
            {
                status[0] = data[0];
                status[1] = data[1];
                status[2] = data[2];
                status[3] = data[3];
            }
            else if ((data[1] & 0x80) != 0)
            {
                status[0] = data[1];
                status[1] = data[2];
                status[2] = data[3];
                status[3] = data[0];
            }
            else if ((data[2] & 0x80) != 0)
            {
                status[0] = data[2];
                status[1] = data[3];
                status[2] = data[0];
                status[3] = data[1];
            }
            else if ((data[3] & 0x80) != 0)
            {
                status[0] = data[3];
                status[1] = data[0];
                status[2] = data[1];
                status[3] = data[2];
            }
            else
            {
                throw new Exception("LCD�X�e�[�^�X�̓ǂݎ�蒆�ɕs���ȃG���[���������܂����B");
            }

            return status;
        }

        private ScreenMode GetScreenMode(int rowCount, int columnCount)
        {
            switch (rowCount)
            {
                case 1:
                    if (columnCount == 80)
                    {
                        return ScreenMode.Screen80x1;
                    }
                    else
                    {
                        throw new ArgumentException("LCD�̍s�񐔂��s���ł��B");
                    }
                case 2:
                    if (columnCount == 40)
                    {
                        return ScreenMode.Screen40x2;
                    }
                    else
                    {
                        throw new ArgumentException("LCD�̍s�񐔂��s���ł��B");
                    }
                case 4:
                    switch (columnCount)
                    {
                        case 8:
                            return ScreenMode.Screen8x4;
                        case 10:
                            return ScreenMode.Screen10x4;
                        case 16:
                            return ScreenMode.Screen16x4;
                        case 20:
                            return ScreenMode.Screen20x4;
                        default:
                            throw new ArgumentException("LCD�̍s�񐔂��s���ł��B");
                    }
                default:
                    throw new ArgumentException("LCD�̍s�񐔂��s���ł��B");
            }
        }
    }
}
