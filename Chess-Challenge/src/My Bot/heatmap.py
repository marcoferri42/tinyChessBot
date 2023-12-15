'''
    Voglio creare delle heatmap per Octobot, ovvero il valore relativo dei pezzi alla posizione nella scacchiera.
    L'obiettivo di questo script e' raccogliere dati da partite realmente avvenute, scaricate tramite https://www.openingtree.com/,
    e creare degli array 2D (o BitBoard vediamo) da usare poi nella Evaluation di Octobot.
    Il gioco posizionale di Wesley So e' folle, e talvolta molto poco posizionale, esattamente come voglio che giochi Octobot.
'''

class Piece:
    def __init__(self, name : str, squares : dict):
        self.name = name
        self.squares = {}

def getRawGames (colour : str) -> str:
    '''
        ritorna stringa con game ad ogni riga
    '''
    data = ''
    with open('Wesley So-'+colour+'.pgn', 'r') as File :
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

def countSquares(moves : list[str]):
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


whiteRawData = getRawGames('white')
whiteMoves = getMoves(whiteRawData, 'white')
whiteSquares = countSquares(whiteMoves)

print(whiteSquares[0].squares)


# blackRawData = getRawGames('black')
# print(blackRawData)
