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
                    Console.WriteLine("id command");
                    Console.WriteLine("option commands");
                    Console.WriteLine("uciok command");
                    break;

                case "isready":
                    Console.WriteLine("readyok command");
                    break;

                case "ucinewgame":
                    break;

                default:
                    break;
            }
            if (input.Contains("position"))
            {
                var payload = input[9..].Split(" ");

                var fen = payload[1];
                var moves = payload[3..];
                if (payload[2].ToString() == "startpos")
                {

                    moves = payload[4..];
                }

                Console.WriteLine(fen);
                Console.WriteLine(moves);
                Array.ForEach(moves, Console.WriteLine);
            }
        }
    }
}

