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
            pBackLightOff = 0x14,
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
        private enum ScreenMode : byte
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
