# Propuesta · lista de términos bloqueados (HU-16 T010)

Estado: **borrador para revisión**. No aplicada a la configuración.

## Situación actual

- Clave: `Disenos:TerminosBloqueados` (arreglo de textos). Se lee en `src/Api/Comun/TerminosBloqueadosConfiguracion.cs`.
- **La lista actual está vacía**: no existe en `appsettings.json` ni en `appsettings.Development.json`. Solo las pruebas la rellenan (`UseSetting("Disenos:TerminosBloqueados:0", ...)`).
- Aplicación (`MapeoDisenos.ADto`, `src/Aplicacion/Disenos/Disenos.cs:71`): se evalúa al **devolver** el DTO, no al guardar. Campos revisados: **nombre** y **descripción**. No hay etiquetas en el modelo.
- Comparación: `string.Contains(término, OrdinalIgnoreCase)`.
  - Ignora mayúsculas: sí.
  - Ignora acentos: **no** (`Beyoncé` ≠ `Beyonce`).
  - Ignora espacios/símbolos: **no** (`Guns N' Roses` ≠ `Guns N Roses` ≠ `GunsNRoses`).
  - Límite de palabra: **no** (subcadena pura).
- Efecto: solo agrega un aviso al DTO; no bloquea (constitución X).

## Riesgos de la implementación y cambio propuesto

| Riesgo | Ejemplo | Tipo |
|---|---|---|
| Subcadena sin límite de palabra | `Fox` en «Foxtrot», `Vans` en «Evans», `Gap` en «Escape gap», `Mac` en «Macabro», `Sega` en «Segadora» | Falso positivo |
| Sin normalizar acentos | «Metállica», «Beyoncé» vs «Beyonce» | Falso negativo |
| Sin normalizar símbolos/espacios | «AC/DC» vs «ACDC» vs «AC DC», «Guns N' Roses» | Falso negativo |

**Cambio mínimo recomendado** (en `ADto`, o mejor un método `Coincide(texto, término)` en el mismo archivo):
normalizar ambos lados (minúsculas, quitar diacríticos con `NormalizationForm.FormD`, reemplazar todo lo que no sea letra/dígito por un espacio, colapsar espacios) y buscar el término como **palabra/frase completa** (`" " + texto + " "` contiene `" " + término + " "`). Con eso: «AC/DC» = «ac dc», «Beyoncé» = «beyonce», y «Foxtrot» ya no activa «Fox».
Limitación aceptada: no detecta variantes pegadas («GunsNRoses») ni ofuscaciones («N1ke»); como solo avisa, no se justifica más. Requiere pruebas nuevas (acento, símbolo, límite de palabra) y es un cambio de código, así que sería parte de la fase 2 solo si lo apruebas.

## Lista propuesta

`*` = ambiguo (palabra común que puede bloquear diseños legítimos). Tú decides cada uno; mi sugerencia: **excluir los `*`** salvo los marcados «(frase)», donde propongo usar la frase completa.

### Ropa y deportes
Nike, Adidas, Puma, Reebok, Under Armour, New Balance, Converse, Vans*, Fila, Champion*, Supreme*, Off-White, Gucci, Louis Vuitton, Chanel, Prada, Versace, Balenciaga, Burberry, Dior, Hermès, Levi's, Lacoste, Tommy Hilfiger, Ralph Lauren, Calvin Klein, Zara, H&M, Gap*, Carhartt, The North Face (frase), Patagonia*, Columbia*, Billabong, Quiksilver, Volcom, Stüssy, Kappa*, Umbro, Asics, Jordan* (usar «Air Jordan»), Lululemon, Hollister, Abercrombie, Diesel*, Guess*, Hugo Boss, Armani, Yves Saint Laurent, Fendi, Givenchy, Hot Wheels, Harley-Davidson.

### Tecnología y videojuegos
Apple*, iPhone, iPad, MacBook, Google, Microsoft, Windows*, Xbox, PlayStation, Nintendo, Sega*, Atari, Samsung, Sony, Netflix, Amazon*, Tesla, Facebook, Instagram, TikTok, WhatsApp, Twitter, YouTube, Spotify, Intel, Nvidia, AMD*, Steam*, Discord*, Twitch, Fortnite, Minecraft, Roblox, Pokémon, Pokemon, Mario Bros, Super Mario, Zelda*, Sonic*, Pac-Man, Tetris, Call of Duty, Grand Theft Auto, Counter-Strike, League of Legends, Valorant, Overwatch, World of Warcraft, Halo*, Mortal Kombat, Street Fighter, Final Fantasy, Resident Evil, Metal Gear, Assassin's Creed, FIFA, EA Sports, Epic Games, Blizzard, Riot Games, Among Us, Genshin Impact, Dark Souls, Elden Ring, The Witcher, Cyberpunk 2077, Animal Crossing, Kirby*, Fall Guys, Free Fire.

### Estudios y franquicias de cine
Disney, Pixar, Marvel, DC Comics, Warner Bros, Universal Studios, Paramount, 20th Century, Lucasfilm, DreamWorks, Illumination, Sony Pictures, Star Wars, Star Trek, Harry Potter, Hogwarts, El Señor de los Anillos, Lord of the Rings, Hobbit*, Matrix*, Jurassic Park, Jurassic World, Avengers, Vengadores, Los Simpson, Back to the Future, Volver al Futuro, Terminator*, Alien*, Depredador, Predator*, Rocky*, Rambo, Indiana Jones, Piratas del Caribe, Fast & Furious, Rápido y Furioso, John Wick, Mad Max, Blade Runner, Ghostbusters, Cazafantasmas, Batman, Superman, Spider-Man, Hombre Araña, Iron Man, Capitán América, Thor*, Hulk, Deadpool, Wolverine, Joker*, Harley Quinn, Wonder Woman, Mujer Maravilla, Aquaman, Flash*, Venom*, Thanos, Groot, Barbie*, Oppenheimer*, Coco* (Disney-Pixar), Frozen, Toy Story, Shrek, Minions, Kung Fu Panda, Madagascar*.

### Series y anime
Game of Thrones, Juego de Tronos, Breaking Bad, Stranger Things, The Walking Dead, La Casa de Papel, Money Heist, Squid Game, El Juego del Calamar, Friends*, The Office*, Peaky Blinders, Narcos, Dark*, Vikings*, The Mandalorian, Baby Yoda, Grogu, Rick and Morty, Dragon Ball, Dragon Ball Z, Goku, Vegeta, Naruto, Sasuke, One Piece, Luffy, Bleach*, Attack on Titan, Shingeki no Kyojin, Death Note, Demon Slayer, Kimetsu no Yaiba, Jujutsu Kaisen, My Hero Academia, Boku no Hero, Fullmetal Alchemist, Sailor Moon, Saint Seiya, Caballeros del Zodiaco, Evangelion, Dr. Stone*, Hunter x Hunter, Tokyo Ghoul, Berserk, Cowboy Bebop, One Punch Man, Chainsaw Man, Spy x Family, Studio Ghibli, Totoro, Spirited Away, El Viaje de Chihiro, Digimon, Yu-Gi-Oh, Beyblade, Pokémon (ver tecnología), El Chavo del Ocho, Chapulín Colorado, Chespirito, La Familia Peluche, Club de Cuervos.

### Personajes de caricaturas
Mickey Mouse, Minnie Mouse, Donald Duck, Pato Donald, Goofy, Pluto*, Winnie the Pooh, Bugs Bunny, Daffy Duck, Porky Pig, Looney Tunes, Tom y Jerry, Scooby-Doo, Scooby Doo, Los Picapiedra, Flintstones, Los Supersónicos, Popeye, Betty Boop, Garfield, Snoopy, Charlie Brown, Peanuts*, Hello Kitty, Doraemon, Pikachu, Bob Esponja, SpongeBob, Patricio Estrella, Padre de Familia, Family Guy, South Park, Futurama, Hora de Aventura, Adventure Time, Steven Universe, Los Padrinos Mágicos, Dexter*, Johnny Bravo, Ben 10, Las Chicas Superpoderosas, Powerpuff, Pinky y Cerebro, Teenage Mutant Ninja Turtles, Tortugas Ninja, Transformers, He-Man, Thundercats, Power Rangers, Gumby, Cabeza de Huevo, Peppa Pig, Paw Patrol, Patrulla Canina, Bluey, Mafalda, Condorito, Memín Pinguín, Kalimán, Sesame Street, Plaza Sésamo, Barney*, Hello Kitty, Sanrio, Care Bears, Osos Cariñosos, Rugrats, Los Thornberrys, Caillou, Dora la Exploradora.

### Bandas y artistas (con foco en México)
**Rock/metal internacional**: Metallica, Iron Maiden, Black Sabbath, Slayer, Megadeth, Pantera, Judas Priest, Motörhead, AC/DC, Led Zeppelin, Pink Floyd, The Rolling Stones, The Beatles, Queen*, Nirvana, Pearl Jam, Soundgarden, Foo Fighters, Red Hot Chili Peppers, Guns N' Roses, Aerosmith, Kiss*, Van Halen, Deep Purple, The Doors, The Who*, The Clash, Ramones, Sex Pistols, Misfits*, Green Day, Blink-182, My Chemical Romance, Linkin Park, System of a Down, Rage Against the Machine, Slipknot, Korn, Tool*, Deftones, Radiohead, Coldplay, Arctic Monkeys, The Strokes, Oasis*, Muse*, U2, Depeche Mode, The Cure, Joy Division, Gorillaz, Bring Me the Horizon, Mägo de Oz, Rammstein, Ghost*, Avenged Sevenfold, Dream Theater, Twenty One Pilots, Imagine Dragons, Paramore, Evanescence, Bon Jovi, Scorpions, Dire Straits, David Bowie, Jimi Hendrix, Bob Marley, Elvis Presley, Michael Jackson, Prince*, Madonna*.
**Rock en español**: Caifanes, Café Tacvba, Maná, Molotov, El Tri, Enjambre, Zoé, Jaguares, Héroes del Silencio, Soda Stereo, Charly García, Los Fabulosos Cadillacs, Los Prisioneros, Panda*, Moderatoduro, Los Auténticos Decadentes, Ratones Paranoicos, Titán*, Maldita Vecindad, Gustavo Cerati, Kinky*, Control Machete, Natalia Lafourcade, Juanes, Shakira.
**Pop/urbano/reggaetón**: Taylor Swift, Beyoncé, Rihanna, Lady Gaga, Ariana Grande, Billie Eilish, Dua Lipa, Katy Perry, Selena Gomez, Justin Bieber, Harry Styles, One Direction, BTS, Blackpink, Bad Bunny, J Balvin, Karol G, Daddy Yankee, Anuel AA, Peso Pluma, Natanael Cano, Junior H, Eslabon Armado, Fuerza Regida, Grupo Frontera, Xavi*, Luis R Conriquez, Gabito Ballesteros, Christian Nodal, Eden Muñoz, Ángela Aguilar, Pepe Aguilar, Vicente Fernández, Alejandro Fernández, Selena Quintanilla, Luis Miguel, Juan Gabriel, Gloria Trevi, Thalía, Paulina Rubio, Belanova, RBD, Intocable, Los Tigres del Norte, Los Ángeles Azules, Grupo Firme, Banda MS, La Arrolladora, Calibre 50, Marca MP, Los Bukis, Los Temerarios, Selena.
**Hip hop**: Eminem, Kendrick Lamar, Drake*, Kanye West, Travis Scott, Tupac, Notorious B.I.G., Snoop Dogg, Wu-Tang Clan, Run-DMC, Cypress Hill, Bad Bunny (ver arriba), Residente, Calle 13, Nicki Minaj, Cardi B, Post Malone, Juice WRLD, Lil Peep, Rich Brian*.
**Festivales/sellos**: Vive Latino, Corona Capital, Coachella, Lollapalooza, Tomorrowland, Warner Music, Universal Music, Sony Music.

### Fútbol, equipos y ligas
**Ligas y organismos**: FIFA, UEFA, Liga MX, Champions League, Copa Libertadores, Concacaf, LaLiga, Premier League, Serie A*, Bundesliga, MLS*, NFL, NBA, MLB, NHL, UFC, WWE, Fórmula 1, Formula 1, Olimpiadas*, Mundial Qatar, Selección Mexicana, El Tri (ver bandas).
**Liga MX**: Club América, Chivas, Guadalajara*, Cruz Azul, Pumas, UNAM*, Tigres, Rayados, Monterrey*, Santos Laguna, Toluca*, Pachuca*, León*, Atlas*, Necaxa, Puebla*, Querétaro*, Mazatlán FC, Juárez FC, FC Juárez, Xolos, Tijuana*, San Luis*, Atlético San Luis.
**Internacional**: Real Madrid, FC Barcelona, Barça, Atlético de Madrid, Manchester United, Manchester City, Liverpool*, Chelsea*, Arsenal*, Tottenham, Juventus, AC Milan, Inter de Milán, Bayern Múnich, Borussia Dortmund, PSG, Paris Saint-Germain, Boca Juniors, River Plate, Flamengo, Santos FC, Inter Miami, LA Galaxy.
**Franquicias NBA/NFL/MLB**: Lakers, Chicago Bulls, Golden State Warriors, Celtics, Yankees, Dodgers, Patriots, Cowboys*, Raiders*, Chiefs*, 49ers, Steelers.
**Jugadores**: Messi, Lionel Messi, Cristiano Ronaldo, CR7, Neymar, Mbappé, Chicharito, Hugo Sánchez, Cuauhtémoc Blanco, Maradona, Pelé, Michael Jordan, LeBron James, Kobe Bryant, Canelo Álvarez, Julio César Chávez, Checo Pérez, Lewis Hamilton.

### Bebidas y alimentos
Coca-Cola, Coca Cola, Pepsi, Sprite*, Fanta, Dr Pepper, Jarritos, Sidral Mundet, Boing, Red Bull, Monster Energy, Gatorade, Powerade, Peñafiel, Topo Chico, Corona*, Corona Extra, Modelo*, Modelo Especial, Victoria*, Pacífico*, Tecate, Sol*, Indio*, Dos Equis, XX Lager, Heineken, Budweiser, Bud Light, Stella Artois, Carta Blanca, Bohemia*, Negra Modelo, Michelob, Miller*, Jack Daniel's, Johnnie Walker, Jim Beam, Absolut, Smirnoff, Bacardi, Captain Morgan, Havana Club, Hennessy, Jägermeister, Baileys, Tequila Don Julio, Don Julio, José Cuervo, Jose Cuervo, Patrón*, 1800, Herradura, Sauza, Casamigos, Cazadores, Maestro Dobel, Mezcal Unión, Jimador, Buchanan's, Tequila Cuervo, Starbucks, Nescafé, Nestlé, Oxxo, Bimbo, Gansito, Sabritas, Doritos, Cheetos, Takis, Ruffles, Pringles, Oreo, McDonald's, Burger King, KFC, Pizza Hut, Domino's, Taco Bell, Subway*, Chipotle*, Dunkin, Hershey's, Kinder, Ferrero, Nutella, M&M's, Skittles, Haribo, Mundet, Lala*, Alpura, Herdez, Maruchan, Cup Noodles, Tajín, Valentina*, Chimoltrufia.

## Notas para decidir
1. Los `*` son los candidatos a quitar (o a limitar a una frase: «Air Jordan», «Corona Extra», «Super Mario»). Con el cambio de «palabra completa» muchos dejan de ser problema de subcadena, pero siguen siendo palabras comunes («Apple», «Sol», «Flash»).
2. Hay duplicados menores entre secciones (Pokémon, Hello Kitty, Selena); se limpian al aplicar.
3. La lista no es exhaustiva ni asesoría legal: solo reduce riesgo de revisión. Se puede crecer después sin tocar código.
4. Para aplicarla: `Disenos:TerminosBloqueados` en `appsettings.json` (repositorio público, no hay secretos). Cada elemento es un término; con la normalización propuesta no hace falta listar variantes con acento ni símbolo.
