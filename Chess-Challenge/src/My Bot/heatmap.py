'''
    Voglio creare delle heatmap per Octobot, ovvero il valore relativo dei pezzi alla posizione nella scacchiera.
    L'obiettivo di questo script e' raccogliere dati da partite realmente avvenute, (scaricate tramite https://www.openingtree.com/) e
    creare degli array 2D (o BitBoard vediamo) da usare poi nella Evaluation di Octobot.
    Il gioco posizionale di Wesley So e' folle, e talvolta molto poco posizionale, esattamente come voglio che giochi Octobot.
'''

from collections import OrderedDict
from os import write

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
            if len(line) > 0 and line[0]== "1":
                data += '\n' + line.replace('+','')
        return data

def getMoves(data : str, colour : str) -> list[str]:
    '''
        non efficientissimo ma sticazzi
    '''
    charList = data.split(' ')
    charList = [el for el in charList if "." not in el and el != '']

    if colour == 'white':
        charList = [el for el in charList if charList.index(el) % 2 == 0]
    else:
        charList = [el for el in charList if charList.index(el) % 2 != 0]
    return charList

def countSquares(moves : list[str]) -> list[Piece]:
    '''
        Da tutte le mosse di un colore trova le mosse fatte dai pezzi,
        conta quante volte un pezzo va in un square.
        Ritorna lista di Piece con dict squares popolato.
    '''

    piecePosition = {'Q': 1, 'N': 2, 'K': 3, 'B': 4, 'R':5}
    pieces = [Piece('P', []), Piece('Q',[]), Piece('N', []), Piece('K', []), Piece('B', []), Piece('R', [])]

    for el in moves:
        if el[0] in list(piecePosition.keys()): # per tutti i pezzi
            if el[len(el)-2:] not in pieces[piecePosition.get(el[0])].squares.keys():  
                pieces[piecePosition.get(el[0])].squares[el[len(el)-2:]] = 1     # se coordinata non in dict, aggiungo
            else:
                pieces[piecePosition.get(el[0])].squares[el[len(el)-2:]] += 1    # se coordinata e' in dict aggiorno valore

        else:   # per pedoni
            if el[len(el)-2:] not in pieces[0].squares.keys(): 
                pieces[0].squares[el[len(el)-2:]] = 1    # aggiungo
            else:
                pieces[0].squares[el[len(el)-2:]] += 1   # aggiorno
    
    return pieces

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

def writeMapstoFile(filename : str, piece : Piece):
    f = open(filename, "a")
    f.truncate(0)
    
    for square,value in piece.squares.items():
        f.write(str(square)+ ":" + str(value) + "\n")
    f.close()
    pass


whiteMap = normalizeMap50(
    countSquares(
        getMoves(
            getRawGames('Wesley So-white.pgn'), 'white')))

blackMap = normalizeMap50(
    countSquares(
        getMoves(
            getRawGames('Wesley So-black.pgn'), 'black')))


writeMapstoFile(r"C:\Users\usr\source\repos\tinyChessBot\Chess-Challenge\src\My Bot\maps\whitePawnMap.txt", whiteMap[0])
writeMapstoFile(r"C:\Users\usr\source\repos\tinyChessBot\Chess-Challenge\src\My Bot\maps\whiteQueenMap.txt", whiteMap[1])
writeMapstoFile(r"C:\Users\usr\source\repos\tinyChessBot\Chess-Challenge\src\My Bot\maps\whiteKnightMap.txt", whiteMap[2])
writeMapstoFile(r"C:\Users\usr\source\repos\tinyChessBot\Chess-Challenge\src\My Bot\maps\whiteKingMap.txt", whiteMap[3])
writeMapstoFile(r"C:\Users\usr\source\repos\tinyChessBot\Chess-Challenge\src\My Bot\maps\whiteBishopMap.txt", whiteMap[4])
writeMapstoFile(r"C:\Users\usr\source\repos\tinyChessBot\Chess-Challenge\src\My Bot\maps\whiteRookMap.txt", whiteMap[5])

writeMapstoFile(r"C:\Users\usr\source\repos\tinyChessBot\Chess-Challenge\src\My Bot\maps\blackPawnMap.txt", blackMap[0])
writeMapstoFile(r"C:\Users\usr\source\repos\tinyChessBot\Chess-Challenge\src\My Bot\maps\blackQueenMap.txt", blackMap[1])
writeMapstoFile(r"C:\Users\usr\source\repos\tinyChessBot\Chess-Challenge\src\My Bot\maps\blackKnightMap.txt", blackMap[2])
writeMapstoFile(r"C:\Users\usr\source\repos\tinyChessBot\Chess-Challenge\src\My Bot\maps\blackKingMap.txt", blackMap[3])
writeMapstoFile(r"C:\Users\usr\source\repos\tinyChessBot\Chess-Challenge\src\My Bot\maps\blackBishopMap.txt", blackMap[4])
writeMapstoFile(r"C:\Users\usr\source\repos\tinyChessBot\Chess-Challenge\src\My Bot\maps\blackRookMap.txt", blackMap[5])

# blackRawData = getRawGames('black')
# print(blackRawData)
