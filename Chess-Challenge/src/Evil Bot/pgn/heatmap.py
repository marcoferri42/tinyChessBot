'''
    Voglio creare delle heatmap per Octobot, ovvero il valore relativo dei pezzi alla posizione nella scacchiera.
    L'obiettivo di questo script e' raccogliere dati da partite realmente avvenute, (scaricate tramite https://www.openingtree.com/) e
    creare degli array 2D (o BitBoard vediamo) da usare poi nella Evaluation di Octobot.
    Il gioco posizionale di Wesley So e' folle, e talvolta molto poco posizionale, esattamente come voglio che giochi Octobot.
'''

from collections import OrderedDict
from multiprocessing.spawn import old_main_modules
from os import write, walk

class Piece:
    def __init__(self, name : str, squares : dict):
        self.name = name
        self.squares = {}

def getRawGames (filename : str) -> str:
    '''
        ritorna stringa con game ad ogni riga
    '''
    data = ''
    with open(filename, 'r') as File :
        for line in File.readlines():
            if len(line) > 0 and line[0] == "1":

                data += '\n' + line.replace('+', '').replace(' 0-1 ', '').replace(' 1-0 ', '').replace(' 1/2-1/2', '').replace(' O-O ', '').replace('O-O-O', '')
        return data

def getMoves(data : str, isWhite: bool) -> list[str]:
    '''
        non efficientissimo ma sticazzi
    '''
    charList = data.split(' ')
    charList = [el for el in charList if "." not in el and el != '']

    if isWhite:
        charList = [el for el in charList if charList.index(el) % 2 == 0]
    else:
        charList = [el for el in charList if charList.index(el) % 2 != 0]

    return charList


def countSquares(moves: list[str]) -> list[Piece]:
    '''
        Da tutte le mosse di un colore trova le mosse fatte dai pezzi,
        conta quante volte un pezzo va in un square.
        Ritorna lista di Piece con dict squares popolato.
    '''
    pieces = {name: Piece(name, {}) for name in ['Q', 'N', 'K', 'B', 'R']}

    for move in moves:
        piece_name = move[0]
        square = move[-2:]

        if piece_name in pieces:
            piece = pieces[piece_name]
            piece.squares[square] = piece.squares.get(square, 0) + 1

    return list(pieces.values())

def fillMaps(squares: dict) -> dict:
    letters = 'abcdefgh'
    for char in letters:
        for num in range(1, 9):
            if char+str(num) not in list(squares.keys()):
                squares[char+str(num)] = 0

    return squares
        

def normalizeMap50(pieces : list[Piece]) -> list[Piece]:
    '''
        normalizza a 50 il massimo
    '''
    for piece in pieces:
        maxvalue = 50/max(piece.squares.values())
        for el in list(piece.squares.keys()):
            piece.squares[el] = int(piece.squares[el] * maxvalue)
        
        piece.squares = fillMaps(piece.squares)
        piece.squares = dict(OrderedDict(sorted(piece.squares.items())))
    
    return pieces

def writeMapstoFile(filename: str, piece: Piece, isWhite: bool):
    old_values = {}

    # Read existing values from the file
    try:
        with open(filename, "r") as f:
            for line in f:
                square, value = line.strip().split(':')
                old_values[square] = int(float(value))
    except FileNotFoundError:
        pass  # File doesn't exist yet, no biggie

    with open(filename, "w") as f:
        for square, new_value in piece.squares.items():
            # Determine sign
            sign = 1 if isWhite or new_value == 0 else -1
            new_value *= sign

            # Calculate average
            if square in old_values:
                average_value = int((int(old_values[square]) + new_value) / 2)
            else:
                average_value = new_value

            f.write(f"{square}:{average_value}\n")



filenames = next(walk('.'), (None, None, []))[2]

for filename in filenames:
    data = normalizeMap50(
            countSquares(
                getMoves(
                    getRawGames(filename), filename.__contains__('white'))));

    color = 'white' if filename.__contains__('white') else 'black' 

    for (i, pieceType) in enumerate(['Q','K','N','B','R']):
        writeMapstoFile(r"C:\Users\usr\source\repos\tinyChessBot\Chess-Challenge\src\My Bot\pgn\maps\\"+color+pieceType+"Map.txt", data[i], filename.__contains__('white'))
