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
        /// カナLCDのコマンドコード
        /// </summary>
        private enum Command : byte
        {
            /// <summary>何もしない: Busy 状態を確認する為のコマンド (NUL)</summary>
            Null = 0x00,
            /// <summary>初期化 (SOH)</summary>
            Initialize = 0x01,
            /// <summary>テキスト転送 (STX)</summary>
            SendText = 0x02,
            /// <summary>画面クリア (FF)</summary>
            ClearScreen = 0x0C,
            /// <summary>Backlight ON  (DC2)</summary>
            BackLightOn = 0x12,
            /// <summary>Backlight OFF (DC4)</summary>
            BackLightOff = 0x14,
            /// <summary>エンコード設定 (0x30～0x35)</summary>
            EncodingSetting = 0x30,
            /// <summary>カーソル移動:右 (CUF)</summary>
            MoveRight = 0x43,
            /// <summary>カーソル移動:左 (CUB)</summary>
            MoveLeft = 0x44,
            /// <summary>カーソル位置 y(0～3), x(0～39) (CUP) ※行(row)指定が先。桁(col)指定は後。省略した時は 0 指定した物とみなす。</summary>
            Locate = 0x48,
            /// <summary>点滅 OFF：デフォルト</summary>
            BlinkOff = 0x70,
            /// <summary>点滅 ON</summary>
            BlinkOn = 0x71,
            /// <summary>カーソル表示 OFF：デフォルト</summary>
            CursorOff = 0x72,
            /// <summary>カーソル表示 ON</summary>
            CursorOn = 0x73,
            /// <summary>画面表示 OFF</summary>
            DisplayOff = 0x74,
            /// <summary>画面表示 ON：デフォルト</summary>
            DisplayOn = 0x75,
            /// <summary>自動スクロール OFF：デフォルト</summary>
            AutoScrollOff = 0x76,
            /// <summary>自動スクロール ON</summary>
            AutoScrollOn = 0x77,
            /// <summary>左から右へカーソルが進む：デフォルト</summary>
            LeftToRight = 0x78,
            /// <summary>右から左へカーソルが進む</summary>
            RightToLeft = 0x79,
            /// <summary>カーソル及び画面スクロールをホーム位置へ戻す</summary>
            RetHome = 0x7A,
            /// <summary>ユーザー定義文字フォントを設定</summary>
            SetUserFont = 0x7B,
            /// <summary>画面スクロール:左 (SL)</summary>
            DisplayScrollLeft = 0xC0,
            /// <summary>画面スクロール:右 (SR)</summary>
            DisplayScrollRight = 0xC1,
            /// <summary>任意の命令列  : RS=0</summary>
            InstructionStream = 0xFE,
            /// <summary>任意のデータ列: RS=1</summary>
            DataStream = 0xFF,
        }

        /// <summary>
        /// カナLCDのステータスコード
        /// </summary>
        private enum Status
        {
            /// <summary>正常</summary>
            Normal,
            /// <summary>初期化されていない</summary>
            NotInitialized
        }

        /// <summary>
        /// カナLCDの画面モード
        /// 注意：この画面サイズは、あくまで内部のDDRAMメモリの設定であり、液晶画面に表示される実際の画面サイズとは違っている。
        ///       例えば、SD1602H なら、DDRAMメモリ的には 40桁×2行あるが、実際に表示されるのは、40桁×2行の内の16桁×2行だけであり、
        ///       残りの24桁×2行の内容を見たいのなら、scrollDisplayLeft/Right 関数で左右にスクロールさせる必要がある。
        /// </summary>
        public enum ScreenMode : byte
        {
            /// <summary>80桁×1行, フォント5x8,  使用するDDRAMアドレス：0x00～0x4F  ※本シールド裏面の SJ2 がショート、SJ1 がオープンの時は、強制的にこのモードになる</summary>
            Screen80x1 = 0x00,
            /// <summary>80桁×1行, フォント5x10, 使用するDDRAMアドレス：0x00～0x4F  ※本シールド裏面の SJ2 がショート、SJ1 もショートの時は、強制的にこのモードになる</summary>
            Screen80x1L = 0x10,
            /// <summary>40桁×2行, フォント5x8,  使用するDDRAMアドレス：0x00～0x27/0x40～0x67</summary>
            Screen40x2 = 0x20,
            /// <summary>8桁×4行, フォント5x8,  使用するDDRAMアドレス：0x00～0x07/0x40～0x47/0x08～0x0F/0x48～0x4F</summary>
            Screen8x4 = 0x30,
            /// <summary>10桁×4行, フォント5x8,  使用するDDRAMアドレス：0x00～0x09/0x40～0x49/0x0A～0x13/0x4A～0x53</summary>
            Screen10x4 = 0x40,
            /// <summary>16桁×4行, フォント5x8,  使用するDDRAMアドレス：0x00～0x0F/0x40～0x4F/0x10～0x1F/0x50～0x5F</summary>
            Screen16x4 = 0x50,
            /// <summary>20桁×4行, フォント5x8,  使用するDDRAMアドレス：0x00～0x13/0x40～0x53/0x14～0x27/0x54～0x67</summary>
            Screen20x4 = 0x60,
            /// <summary>40桁×4行, フォント5x8,  使用するDDRAMアドレス：0x00～0x27/0x40～0x67/0x00～0x27/0x40～0x67  ※本シールドでは非対応</summary>
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
                throw new ArgumentOutOfRangeException("インデックスの値が不正です。インデックスは0～7の範囲である必要があります。");
            }

            if (fontData.Length != 8)
            {
                throw new ArgumentException("フォントデータは要素長8のbyte型配列である必要があります");
            }

            Write((byte)Command.SetUserFont, index, fontData[7], fontData[6], fontData[5], fontData[4], fontData[3], fontData[2], fontData[1], fontData[0]);
            Thread.Sleep(2);
        }

        public void SetCursor(byte row, byte column)
        {
            if (column >= columnCount || row >= rowCount)
            {
                throw new ArgumentOutOfRangeException($"行・列の指定が不正です。行は0～{rowCount}、列は0～{columnCount}の間でなければなりません。");
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
                // 1回の送信で最大127バイトまでに制限
                if (messageLength > (LcdCommandMax - 1))
                {
                    messageLength = LcdCommandMax - 1;
                }

                // メッセージ+コマンド1byte
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

            // 読み取った４つの data の中で、bit7 が 1 になっている data が、開始 status です
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
                throw new Exception("LCDステータスの読み取り中に不明なエラーが発生しました。");
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
                        throw new ArgumentException("LCDの行列数が不正です。");
                    }
                case 2:
                    if (columnCount == 40)
                    {
                        return ScreenMode.Screen40x2;
                    }
                    else
                    {
                        throw new ArgumentException("LCDの行列数が不正です。");
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
                            throw new ArgumentException("LCDの行列数が不正です。");
                    }
                default:
                    throw new ArgumentException("LCDの行列数が不正です。");
            }
        }
    }
}
