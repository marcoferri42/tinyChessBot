using ChessChallenge.Chess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


internal class Cli
{
    public static void Mainssss(string[] args)
    {
        var OctoBot = new OctoBot();
        var fen = "";
        var moves = new string[60];

        Console.WriteLine("                    dP            dP                  dP  \r\n                    88            88                  88  \r\n.d8888b. .d8888b. d8888P .d8888b. 88d888b. .d8888b. d8888P\r\n88'  `88 88'  `\"\"   88   88'  `88 88'  `88 88'  `88   88  \r\n88.  .88 88.  ...   88   88.  .88 88.  .88 88.  .88   88  \r\n`88888P' `88888P'   dP   `88888P' 88Y8888' `88888P'   dP  ");
        while (true)
        {
            var input = Console.ReadLine();

            switch (input)
            {
                case "uci":
                    Console.WriteLine("id OctoBot.v1");
                    Console.WriteLine("id name OctoBot");
                    Console.WriteLine("id author matifema");
                    Console.WriteLine("option");
                    Console.WriteLine("uciok");
                    break;

                case "isready":
                    Console.WriteLine("readyok");
                    break;

                case "ucinewgame":
                    fen = "";
                    moves = new string[60];
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
            if (input.Contains("position"))
            {
                var payloadIndex = input.IndexOf("moves ");
                if (payloadIndex != -1)
                {
                    fen = input.Substring(9, payloadIndex - 9).Trim();
                    moves = input.Substring(payloadIndex + 6).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                }
            }

            if (input.Count() >= 1 && input[..2].ToString() == "go")
            {
                // start engine
                OctoBot.CliThink(fen, moves);
            }
        }
    }
}

