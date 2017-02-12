using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Cir_AutoGen
{
    class Program
    {

        static void Main(string[] args)
        {
            bool restart = false;
            while (true)
            {
                restart = false;
                int width = 0;
                int height = 0;
                int minblocks = 0;
                int maxblocks = 0;
                int blocks = 0;
                int mindiff = 0;
                int[,] stageMap;
                int skipfreq = 100;

                string bestStageQuery = "";
                int bestStageMoves = 0;

                Console.WriteLine("Cir-ランダム出力プログラム");
                Console.WriteLine();
                Console.WriteLine("3x3以上で指定");
                bool parseresult = false;
                while (!parseresult || !(width > 2))
                {
                    Console.Write("幅を入力（ボーダーを除く）:");
                    parseresult = int.TryParse(Console.ReadLine(), out width);
                }
                parseresult = false;
                while (!parseresult || !(height > 2))
                {
                    Console.Write("高さを入力（ボーダーを除く）:");
                    parseresult = int.TryParse(Console.ReadLine(), out height);
                }

                Console.WriteLine("{0} x {1} の盤面が指定されました。", width, height);



                parseresult = false;
                while (!parseresult || !(minblocks > 0))
                {
                    Console.Write("ブロックの配置最小個数を入力：", (width - 1) * (height - 1) - 2);
                    parseresult = int.TryParse(Console.ReadLine(), out minblocks);
                }
                parseresult = false;
                while (!parseresult || (!(maxblocks <= (width - 1) * (height - 1) - 2) && !(minblocks < maxblocks)))
                {
                    Console.Write("ブロックの配置最小個数を入力({0}以内)：", (width - 1) * (height - 1) - 2);
                    parseresult = int.TryParse(Console.ReadLine(), out maxblocks);
                }
                parseresult = false;
                while (!parseresult || !(mindiff >= 0))
                {
                    Console.Write("生成されたステージのうち、最短解がこの数以下のものはスキップする：");
                    parseresult = int.TryParse(Console.ReadLine(), out mindiff);
                }
                parseresult = false;
                while (!parseresult || !(skipfreq > 0))
                {
                    Console.Write("一時停止する頻度：");
                    parseresult = int.TryParse(Console.ReadLine(), out skipfreq);
                }

                Console.Write("いずれかのキーを入力して開始");
                Console.ReadLine();

                int skipCount = 0;
                while (true)
                {
                    Console.Clear();
                    Console.WriteLine(skipCount);

                    //マップ配列を1(空白)で塗りつぶし
                    stageMap = new int[width, height];
                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            stageMap[j, i] = 1;
                        }
                    }
                    Random r = new System.Random();

                    //開始座標を決定
                    int stx = r.Next(width);
                    int sty = r.Next(height);
                    stageMap[stx, sty] = 3;
                    //Console.WriteLine("開始位置：({0},{1})", stx, sty);
                    //ゴール座標を決定
                    int glx;
                    int gly;
                    while (true)
                    {
                        glx = r.Next(width);
                        gly = r.Next(height);
                        if (glx != stx && gly != sty) break;
                    }
                    stageMap[glx, gly] = 4;
                    //Console.WriteLine("ゴール位置：({0},{1})", glx, gly);

                    blocks = minblocks + r.Next(maxblocks - minblocks + 1);

                    for (int i = 0; i < blocks; i++)
                    {
                        int blx;
                        int bly;
                        while (true)
                        {
                            blx = r.Next(width);
                            bly = r.Next(height);
                            if (stageMap[blx, bly] == 1) break;
                        }
                        stageMap[blx, bly] = 2;
                        //Console.WriteLine("ブロック座標：（{0}, {1}）", blx, bly);
                    }
                    //Console.WriteLine("クリアの可否をチェックします。");
                    string StageText = "";
                    if (width.ToString().Length == 1)
                    {
                        StageText = "0" + width.ToString();

                    }
                    else
                    {
                        StageText = width.ToString();
                    }
                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            StageText += stageMap[j, i];
                        }
                    }
                    StageStruct Stage = new StageStruct(StageText, "Auto-Generated", "Generated Automatically by Cir-Gen", 0);
                    UTJ.Board Board = new UTJ.Board(Stage);
                    Board.solve();
                    if (Board.isSolvable() == null)
                    {
                        //Console.Clear();
                        if (!(Board.SolutionLength <= mindiff))
                        {
                            Console.WriteLine("クリア可能なステージが生成されました！");
                            Console.WriteLine("ステージ情報：");
                            Console.WriteLine("　インナーサイズ：{0}x{1}", width, height);
                            Console.WriteLine("　回転回数：{0}", Board.SolutionLength);
                            Console.WriteLine();

                            string versionstring = "003";
                            string textstring = StageText;
                            string titlestring = HttpUtility.UrlEncode(Stage.StageTitle);
                            string descriptionstring = HttpUtility.UrlEncode(Stage.StageDescription);
                            string turncountstring = "0";
                            string returnQuery = "?v=" + versionstring + "&s=" + textstring + "&t=" + titlestring + "&d=" + descriptionstring + "&c=" + turncountstring;

                            Console.WriteLine(returnQuery);
                            Console.WriteLine();
                            Console.Write("もう一度生成しますか？[y/n]：");
                            if (Console.ReadLine() == "y")
                            {

                            }
                            else
                            {
                                Console.Write("パラメータを変更しますか？[y/n]：");
                                if (Console.ReadLine() == "y")
                                {
                                    restart = true;
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        else
                        {
                            //簡単すぎるステージ
                            if(Board.SolutionLength > bestStageMoves)
                            {
                                //今までで最難のステージ
                                bestStageMoves = Board.SolutionLength;
                                string versionstring = "003";
                                string textstring = StageText;
                                string titlestring = HttpUtility.UrlEncode(Stage.StageTitle);
                                string descriptionstring = HttpUtility.UrlEncode(Stage.StageDescription);
                                string turncountstring = "0";
                                string returnQuery = "?v=" + versionstring + "&s=" + textstring + "&t=" + titlestring + "&d=" + descriptionstring + "&c=" + turncountstring;

                                bestStageQuery = returnQuery;
                            }
                            skipCount++;
                            if (skipCount % skipfreq == 0)
                            {
                                Console.WriteLine(skipfreq.ToString() + "回スキップを行いました。");
                                Console.WriteLine("最難ステージ：");
                                Console.WriteLine(bestStageQuery);
                                Console.WriteLine("移動回数：" + bestStageMoves);
                                if (Console.ReadKey().Key == ConsoleKey.Escape)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {

                        //Console.Clear();
                        //Console.WriteLine(Board.isSolvable());
                        Console.WriteLine("クリア不能");
                        //Console.Write("もう一度生成しますか？[y/n]：");
                        /*if(Console.ReadLine() == "y")
                        {

                        }else
                        {
                            break;
                        }*/
                        skipCount++;
                        if (skipCount % skipfreq == 0)
                        {
                            Console.WriteLine(skipCount.ToString() + "回スキップを行いました。");
                            Console.WriteLine("最難ステージ：");
                            Console.WriteLine(bestStageQuery);
                            Console.WriteLine("移動回数：" + bestStageMoves);
                            if (Console.ReadKey().Key == ConsoleKey.Escape)
                            {
                                break;
                            }
                        }
                    }
                    

                }//while
                if (restart == false)
                {
                    
                    //Console.WriteLine("終了します。");
                    Console.ReadLine();
                    //break;
                }

            }//while
        }//main
    }//class
}//namespace
