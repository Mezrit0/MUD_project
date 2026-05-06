Tento projekt je textová hra pro více hráčů (MUD) s tematikou Cyberpunku. Aplikace využívá architekturu Server-Klient a komunikaci přes TCP sockety.

Jak projekt spustit
1. Spuštění Serveru
Přejděte do složky: MUD_project/bin/Debug/net8.0/

Spusťte soubor: MUD_project.exe

Server po úspěšném načtení světa vypíše: Server bezi na portu 1235

2. Spuštění Klienta
Přejděte do složky: MUD_Client/bin/Debug/net8.0/

Spusťte soubor: MUD_Client.exe

Po vyzvání zadejte údaje pro připojení:

IP adresa: 127.0.0.1

Port: 1235

Dostupné příkazy ve hře
Příkazy lze zadávat po přihlášení a výpisu hlášky --- READY ---:

scan – Zobrazí popis aktuální místnosti, předměty na zemi a východy

go [smer] – Přesun do jiné místnosti (např. go north)

take [predmet] – Sebere předmět z místnosti do inventáře (např. take chip)

inv – Zobrazí obsah vašeho inventáře

exit – Bezpečné odpojení a uložení stavu postavy do JSONu

Technické informace
I1 (Nacteni sveta): Herní svět je definován v souboru world.json

I2 (Logovani): Aktivita hráčů se v reálném čase vypisuje do konzole serveru

I3 (Persistence): Data hráčů se ukládají do .json souborů při odhlášení

I4 (Klient): Vlastní klientský program pro komunikaci se serverem
