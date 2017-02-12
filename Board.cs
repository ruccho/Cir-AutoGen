//using UnityEngine;
using System.Collections.Generic;

using System;
using System.Web;

namespace UTJ
{

    class Board
    {
        //ブロックタイプ
        private enum Type
        {
            None = 0,
            E = 1,                  // empty
            B = 2,                  // block
            S = 3,                  // start
            G = 4,                  // goal
            K = 5,                  // key
            O = 6,                  // door
            U = 7,                  // up
            L = 8,                  // left
            D = 9,                  // down
            R = 0xa,                // right
        }
        private enum Direction
        {
            Up,
            Left,
            Down,
            Right,
        }

        //対象のStage構造体
        private StageStruct stage_;
        //ステージの構造を保持
        private Type[,] board_;
        private int width_;
        private int height_;
        //幅か高さ、大きい方を保持
        private int max_;
        //基本的に下
        private Direction dir_;
        private int player_x_;
        private int player_y_;
        private bool door_exist_;
        private int door_x_;
        private int door_y_;
        //回転制限
        private int max_move_;
        private Direction[] solusion_;


        public Board(StageStruct stage)
        {
            //各種プロパティの初期化
            stage_ = stage;
            width_ = stage_.StageWidth;
            height_ = stage_.StageHeight;
            max_ = width_ > height_ ? width_ : height_;
            dir_ = Direction.Down;
            board_ = new Type[width_, height_];
            door_exist_ = false;
            door_x_ = -1;
            door_y_ = -1;
            max_move_ = stage.StageTurnCount;
            if (max_move_ <= 0)
            {
                max_move_ = System.Int32.MaxValue;
            }
            //max_move_ = System.Int32.MaxValue;
            solusion_ = null;

            string body = stage_.StageBody;
            var idx = 0;
            for (var y = 0; y < height_; ++y)
            {
                for (var x = 0; x < width_; ++x)
                {
                    var ch = body[idx];
                    ++idx;
                    Type type = Type.None;
                    if ('1' <= ch && ch <= '9')
                    {
                        type = (Type)(ch - '0');
                    }
                    else if (ch == 'a')
                    {
                        type = (Type)(0xa);
                    }
                    else
                    {
                        Console.WriteLine(ch);
                        Console.WriteLine(false);
                    }
                    int x0 = x;
                    int y0 = (height_ - y - 1);
                    if (type == Type.S)
                    {
                        player_x_ = x0;
                        player_y_ = y0;
                        type = Type.E;
                    }
                    else if (type == Type.O)
                    {
                        door_x_ = x0;
                        door_y_ = y0;
                        door_exist_ = true;
                        type = Type.E;
                    }
                    board_[x0, y0] = type;
                }
            }
        }

        private void dump()
        {
            //ステージ構造をログ
            for (var y = 0; y < height_; ++y)
            {
                string buf = "";
                for (var x = 0; x < width_; ++x)
                {
                    buf += string.Format("{0}", board_[x, y]);
                }
                //Console.WriteLine(buf);
            }
            //Console.WriteLine("({0},{1}), {2}", player_x_, player_y_, stage_.StageTitle);
        }

        private int calcId()
        {
            return calcId(player_x_, player_y_, dir_, door_exist_);
        }

        private int calcId(int px, int py, Direction dir, bool door_exist)
        {
            // |=はOR代入演算子。両辺でOR演算をした結果をvalに代入・返す
            int val = 0;
            val |= px << 0;
            val |= py << 8;
            val |= ((int)dir) << 16;
            val |= (door_exist ? 1 : 0) << 24;
            return val;
        }

        private int try_fall(Direction dir, ref int px, ref int py, ref bool door_exist, out bool solved)
        {
            int vx = 0;
            int vy = 0;
            Type counter_dir = Type.None;
            switch (dir)
            {
                case Direction.Up:
                    vx = 0;
                    vy = -1;
                    counter_dir = Type.D;
                    break;
                case Direction.Left:
                    vx = -1;
                    vy = 0;
                    counter_dir = Type.R;
                    break;
                case Direction.Down:
                    vx = 0;
                    vy = 1;
                    counter_dir = Type.U;
                    break;
                case Direction.Right:
                    vx = 1;
                    vy = 0;
                    counter_dir = Type.L;
                    break;
            }

            solved = false;
            for (var i = 0; i < max_; ++i)
            {
                int nx = px + vx;
                int ny = py + vy;
                if (nx < 0 || width_ <= nx ||
                    ny < 0 || height_ <= ny)
                {
                    break;
                }
                Type nt = board_[nx, ny];
                if (nt == Type.G)
                {
                    solved = true;
                    break;
                }
                if (nt == Type.B || nt == counter_dir)
                {
                    break;
                }
                else if (door_exist && nx == door_x_ && ny == door_y_)
                {
                    break;
                }
                else if (door_exist && nt == Type.K)
                {
                    door_exist = false;
                }
                px = nx;
                py = ny;
            }

            return calcId(px, py, dir, door_exist);
        }

        private Direction turnL(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:
                    return Direction.Right;
                case Direction.Left:
                    return Direction.Up;
                case Direction.Down:
                    return Direction.Left;
                case Direction.Right:
                    return Direction.Down;
            }
            //Console.WriteLine(false);
            return Direction.Down;
        }
        private Direction turnR(Direction dir)
        {
            switch (dir)
            {
                case Direction.Up:
                    return Direction.Left;
                case Direction.Left:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Up;
            }
            //Console.WriteLine(false);
            return Direction.Down;
        }

        private void regist_result(List<Direction> result)
        {
            if (solusion_ == null || solusion_.Length > result.Count)
            {
                solusion_ = new Direction[result.Count];
                for (var i = 0; i < result.Count; ++i)
                {
                    solusion_[i] = result[i];
                }
            }
        }

        private void solve_branch(Direction selected_move,
                                  List<Direction> result, Dictionary<int, int> hash,
                                  Direction dir, int px, int py, bool door_exist,
                                  int stay_count, int move_count)
        {
            result.Add(selected_move);
            Direction dir0 = (selected_move == Direction.Left ? turnL(dir) : turnR(dir));
            int px0 = px;
            int py0 = py;
            bool door_exist0 = door_exist;
            bool solved;
            int id = try_fall(dir0, ref px0, ref py0, ref door_exist0, out solved);
            if (solved)
            {
                if (move_count < max_move_)
                {
                    max_move_ = move_count;
                }
                regist_result(result);
            }
            else
            {
                bool repeat_failure = false;
                if (px0 == px && py0 == py)
                {
                    ++stay_count;
                    if (stay_count >= 2)
                    {
                        repeat_failure = true;
                    }
                }
                else
                {
                    stay_count = 0;
                }
                if (!repeat_failure)
                {
                    if (!hash.ContainsKey(id) || hash[id] > move_count)
                    {
                        hash[id] = move_count;
                        internal_solve(result, hash, dir0, px0, py0, door_exist0, stay_count, move_count + 1);
                    }
                }
            }
            result.RemoveAt(result.Count - 1);
        }


        private void internal_solve(List<Direction> result, Dictionary<int, int> hash,
                                    Direction dir, int px, int py, bool door_exist,
                                    int stay_count, int move_count)
        {
            if (move_count > max_move_)
            {
                return;
            }

            Direction move = Direction.Right;
            solve_branch(move,
                         result, hash,
                         dir, px, py, door_exist,
                         stay_count, move_count);
            solve_branch(move == Direction.Left ? Direction.Right : Direction.Left,
                         result, hash,
                         dir, px, py, door_exist,
                         stay_count, move_count);
        }

        public string isSolvable()
        {
            var hash = new Dictionary<int, int>();
            Direction dir = dir_;
            int px = player_x_;
            int py = player_y_;
            bool door_exist = door_exist_;
            bool solved;
            int move_count = 1;
            var id = try_fall(dir, ref px, ref py, ref door_exist, out solved);
            if (solved)
            {
                return null;
            }
            hash[id] = move_count;
            var result = new List<Direction>();
            max_move_ = System.Int32.MaxValue;
            internal_solve(result, hash, dir, px, py, door_exist, 0 /* stay_count */, move_count);
            if (solusion_ != null && solusion_.Length > 0 && solusion_.Length <= stage_.StageTurnCount)
            {
                //クリア可能
                return null;
            }
            else if (solusion_ != null && solusion_.Length > 0 && stage_.StageTurnCount <= 0)
            {
                //クリア可能
                return null;
            }
            else if (solusion_ != null && solusion_.Length > stage_.StageTurnCount && stage_.StageTurnCount > 0)
            {
                //回転制限が小さすぎるためにクリア不能
                return "設定された回転回数が少なすぎます。" + solusion_.Length + "以上に設定してください。";
            }
            else
            {
                //構造上クリア不能
                return "ステージの構造上クリアが不可能です。";
            }
        }
        public int SolutionLength
        {
            get
            {
                return solusion_.Length;
            }
        }
        public int TurnCount()
        {
            solve();
            if (solusion_ != null && solusion_.Length > 0)
            {
                //Debug.LogFormat("solved:{0} moves.", solusion_.Length);
                return solusion_.Length;
            }
            else
            {
                return -1;
            }

        }
        public void solve()
        {
            var hash = new Dictionary<int, int>();

            dump();
            Direction dir = dir_;
            int px = player_x_;
            int py = player_y_;
            bool door_exist = door_exist_;
            bool solved;
            int move_count = 1;

            var id = try_fall(dir, ref px, ref py, ref door_exist, out solved);
            if (solved)
            {
                //Console.WriteLine("no need to solve.");
                return;
            }
            hash[id] = move_count;

            var result = new List<Direction>();
            internal_solve(result, hash, dir, px, py, door_exist, 0 /* stay_count */, move_count);

            if (solusion_ != null && solusion_.Length > 0)
            {
                //Console.WriteLine("solved:{0} moves.", solusion_.Length);
                Direction d = solusion_[0];
                int cnt = 1;
                for (var i = 1; i < solusion_.Length; ++i)
                {
                    if (d == solusion_[i])
                    {
                        ++cnt;
                    }
                    else
                    {
                        //Console.WriteLine("{0}:{1}", d, cnt);
                        d = solusion_[i];
                        cnt = 1;
                    }
                }
                //Console.WriteLine("{0}:{1}", d, cnt);
            }
            else
            {
                //Console.WriteLine("no solusion found..");
            }
        }
    }

    
} // namespace UTJ {
public class StageStruct
{
    private bool allowCauseChange = true;
    public bool isValid;
    public int StageVersion;
    public string StageText;
    public string StageTitle;
    public string StageDescription;
    public int StageWidth;
    public int StageHeight;
    public int StageTurnCount;
    public string StageBody
    {
        get
        {
            return StageText.Substring(2);
        }
    }

    const int NEWEST_VERSION = 3;

    public StageStruct(string text, string title, string description, int turncount = 0)
    {
        isValid = true;
        StageVersion = 1;
        StageText = text;
        StageTitle = title;
        StageDescription = description;
        StageWidth = 0;
        StageHeight = 0;
        StageTurnCount = turncount;
        //StageBody = text.Substring(2);
        if (CalcWidthAndHeight(text) != null)
        {
            StageWidth = CalcWidthAndHeight(text)[0];
            StageHeight = CalcWidthAndHeight(text)[1];
        }
        else
        {
            isValid = false;
        }
    }

    public StageStruct(string url)
    {
        //initialize
        isValid = false;
        StageVersion = -1;
        StageText = "";
        StageTitle = "";
        StageDescription = "";
        StageWidth = 0;
        StageHeight = 0;
        StageTurnCount = 0;
        //StageBody = "";


        if (url.IndexOf("v=") == -1)
        {
            isValid = false;
        }
        else
        {
            //バージョンは001,002,のように表記されている
            string VerStr = extractQueryBody(url, 'v');
            //Version = int.Parse(VerStr);
            VerStr = VerStr.TrimStart('0');
            int parseresult;
            if (!(int.TryParse(VerStr, out parseresult)))
            {
                //バージョン指定が適当でない
                isValid = false;
            }
            else
            {
                StageVersion = parseresult;
                if (NEWEST_VERSION < StageVersion)
                {
                    //未対応バージョンなら読み込まない
                    isValid = false;
                    return;
                }

                //タイトル
                StageTitle = HttpUtility.UrlDecode(extractQueryBody(url, 't'));
                //StageTitle = WWW.UnEscapeURL(extractQueryBody(url, 't'));
                StageDescription = HttpUtility.UrlDecode(extractQueryBody(url, 'd'));
                //StageDescription = WWW.UnEscapeURL(extractQueryBody(url, 'd'));
                StageText = extractQueryBody(url, 's');
                if (StageVersion >= 2)
                {
                    if (!(int.TryParse(extractQueryBody(url, 'c'), out parseresult)))
                    {
                        isValid = false;
                        return;
                    }
                    else
                    {
                        StageTurnCount = parseresult;
                    }
                }
                //StageBody = StageText.Substring(2);
                if (StageText == null || StageText == "")
                {
                    isValid = false;
                    return;
                }
                if (CalcWidthAndHeight(StageText) != null)
                {
                    StageWidth = CalcWidthAndHeight(StageText)[0];
                    StageHeight = CalcWidthAndHeight(StageText)[1];
                }
                else
                {
                    isValid = false;
                }
                isValid = true;
            }
        }
    }
    private string extractQueryBody(string url, char QueryLetter)
    {
        int QueryIndex = url.IndexOf(QueryLetter + "=");

        if (QueryIndex == -1)
        {
            return "";
        }
        int nextQueryIndex = url.IndexOf("&", QueryIndex);
        if (nextQueryIndex < 0)
        {
            //末尾のクエリの場合
            nextQueryIndex = url.Length;
        }
        return url.Substring(QueryIndex + 2, nextQueryIndex - QueryIndex - 2);
    }

    private int[] CalcWidthAndHeight(string StageText)
    {
        int StageWidth;
        int StageHeight;
        int parseresult;
        if (!(int.TryParse(StageText.Substring(0, 2), out parseresult))) return null;
        StageWidth = parseresult;

        //３文字目から最後までを切り取り、StageMapに格納
        StageText = StageText.Substring(2);

        //構造文字列の長さがWidthで割り切れるかチェックし、格納
        if (StageText.Length % StageWidth != 0) return null;
        StageHeight = StageText.Length / StageWidth;
        int[] returntext = new int[2];
        returntext[0] = StageWidth;
        returntext[1] = StageHeight;
        return returntext;
    }

}

public class Query/* : MonoBehaviour*/
{

    /* public static string generateQuery(string text, string title, string description)
     {
         //int version = 1;
         string versionstring = "001";

         string textstring = WWW.EscapeURL(text);
         string titlestring = WWW.EscapeURL(title);
         string descriptionstring = WWW.EscapeURL(description);
         string returnQuery = "?v=" + versionstring + "&s=" + textstring + "&t=" + titlestring + "&d=" + descriptionstring + "&c=0";
         return returnQuery;
     }*/
}

