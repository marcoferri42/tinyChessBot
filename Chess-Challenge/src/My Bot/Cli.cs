using ChessChallenge.Chess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


internal class Cli
{
    static public void Main(String[] args)
    {
        var OctoBot = new OctoBot();

        Console.WriteLine("                    dP            dP                  dP  \r\n                    88            88                  88  \r\n.d8888b. .d8888b. d8888P .d8888b. 88d888b. .d8888b. d8888P\r\n88'  `88 88'  `\"\"   88   88'  `88 88'  `88 88'  `88   88  \r\n88.  .88 88.  ...   88   88.  .88 88.  .88 88.  .88   88  \r\n`88888P' `88888P'   dP   `88888P' 88Y8888' `88888P'   dP  ");
        while (true)
        {
            var input = Console.ReadLine();

            switch (input)
            {
                case "uci":
                    Console.WriteLine("id name OctoBot");
                    Console.WriteLine("id author joe");
                    Console.WriteLine("option");
                    Console.WriteLine("uciok");
                    break;

                case "isready":
                    Console.WriteLine("readyok");
                    break;

                case "ucinewgame":
                    // reset board and data from search
                    break;
                case "stop":
                    // stop calculation
                    break;
                case "quit":
                    // quit engine
                    break;
                default:
                    break;
            }
            if (input.Contains("position") && input.Contains(" ") && input.Count()> 10)
            {
                var payload = input[9..].Split(" ");

                var fen = payload[1];
                var moves = payload[3..];
                if (payload[2].ToString() != "move")
                {
                    moves = payload[4..];
                }

                Console.WriteLine(fen);
                Console.WriteLine(moves);
                Array.ForEach(moves, Console.WriteLine);
            }


            if (input.Count() >= 1 && input[..2].ToString() == "go")
            {
                // start engine
                Console.WriteLine("start");
                Console.WriteLine("info depth 1 score cp -1 time 10 nodes 26 nps 633 pv e7e6");
            }
        }
    }
}

