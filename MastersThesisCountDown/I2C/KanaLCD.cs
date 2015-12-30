using System;
using System.Threading;
using Microsoft.SPOT;

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
            pBackLightOff = 0x14,
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
        private enum ScreenMode : byte
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

        private Status status;
        private int columnCount;
        private int rowCount;

        public KanaLCD(ushort address, int columnCount, int rowCount) : base(address, 100, 500)
        {
            Initialize(columnCount, rowCount);
        }



        public void Initialize(int columnCount, int rowCount, int waitTime = 60)
        {
            var screenMode = GetScreenMode(columnCount, rowCount);
            this.columnCount = columnCount;
            this.rowCount = rowCount;

            Thread.Sleep(waitTime);

            Write((byte)Command.Initialize, (byte)screenMode);
        }

        private ScreenMode GetScreenMode(int columnCount, int rowCount)
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
