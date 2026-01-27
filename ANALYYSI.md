# Analyysi tekoälyn käytöstä

## Tekoälyn hyödyt

Tekoäly on hyvin kätevä varsinkin kahdessa käyttötarkoituksessa. Yksinkertaisten toimintojen nopeassa toteutuksessa kuten projektin aloituksessa, ja testien teossa, sekä virheiden ja puutteiden löytämisessä.

Projektin aloitus tekoälyllä oli hyvin yksinkertaista koska sille tarvitsee kertoa, vain mitä tehdään ja millä kielellä, sekä perus toimintoja, jolloin tekoäly pystyttää halutun tuloksen käden käänteessä. Uusien ominaisuuksienkin lisääminen on helppoa, kunhan kuvailee tarpeeksi hyvin, mitä halutaan.

Kun projektissa teki muutoksia controllereihin ja halusin testata niitä, tekoälyllä pystyi tekemään kätevästi ja nopeasti .http fileen api kutsuja joilla testaus sujui mukavasti.

Toinen hyvin hyödyllinen käyttötapaus on puuteiden löytäminen ja koodin laadun valvonta. Käytin tekoälyä analysoimaan koodia ja tekemään sen perusteella ASSESSMENT.md filen johon se teki listan puutteista ja koodin laadun parannus ehdotuksista jonka avulla löysin monia parannuksen kohteita joita en ehkä olisi huomannut tai olin unohtanut.

## Tekoälyn heikkoudet

Tekoäly tekee myös virheitä koska se ei aina ota koko projektia huomioon. Vaikka se on hyvä löytämään puutteita ja laatuvirheitä se ei ole täydellinen niiden ratkomisessa. Tekoäly esimerkiksi ehdotti, että koska malli ei sisältänyt minkäänlaista validointia, sille pitäisi laittaa data annotaatiot sekä tehdä sille OnModelCreating overload DbContextiin. Itsessään se ei ole huono idea mutta koska projektissa käytetään in-memory tietokantaa AI:n suosittelema OnModelCreating ei olisi toiminut sillä se halusi käyttää toimintoja jotka toimisivat ainoastaan relaatiotietokannassa. Päädyin laittamaan tässä tapauksessa vain data annotaatiot.

AI ehdotti että controllerissa rivillä 109 ja 142 olevat error ei kertoisi käyttäjälle mikä arvo ei ole validi vaikka viesti oikeasti muuttuu virheen myötä. Luulen että tämä tapahtuu koska virhe viesti tulee toisesta metodista arvona jota AI ei mahdollisesti osannut yhdistää tähän. AI myös suositteli frontendistä käyttöä varten tehtäviä muutoksia jotka olisivat vain hyödyttömiä tässä projektissa. Näen että isommissa koodikannoissa tällaiset virheet voivat olla suurempi ongelma kun koodin monimutkaisuus kasvaa ja AI:n kyky ottaa se kaikki huomioon laskee.

Tekoälyn käytössä pitää myös huomioida se, että se voi tuottaa eri tuloksen tapauksissa joissa siltä pyydetään samoja asioita. Esimerkiksi ennen tämän projektin tekoa tein yksinkertaisemman testiversion jossa tekoäly loi automaattisesti perus data annotaatiot malliin, mutta tätä projektia tehdessä, se loi vain kommentin siitä, että on mahdollistat lisätä ne myöhemmin, vaikka molemmissa tapauksissa käytin samaa promptia.

## Tärkeimmät muutokset

Tärkein alkuperäisen version jälkeinen muutos on omasta mielestäni aikavyöhykkeiden huomioon ottaminen. On mahdollista että huoneita varataan esim. kansainvälisessä yrityksessä eri puolilta maapalloa, jolloin pelkän paikallisen ajan käyttäminen sotkisi aikataulut. Sen takia varaukset sisältävät varaajan aikavyöhykkeen ja varaus varastoidaan kantaan UTC muodossa. Käytin tämän tekemiseen Ai:ta mutta tein myöhemmin aikavyöhykkeelle vielä validoinnin Post ja Put endpointteihin. Isommassa projektissa jossa olisi kirjautuneita käyttäjiä aikavyöhykkeen voisi laittaa omaan UserInfoServiceen josta sen voisi hakea milloin sitä tarvitsee jolloin sitä ei erikseen tarvitsisi laittaa joka varaukseen.

Toinen tärkeähkö lisäys oli loggaaminen, jolloin saan tietoa virheistä konsoliin. Isommassa projektissa loggaaminen on tärkeää, sillä niin saadaan löydettyä yleisiä virheitä käytössä olevassa ohjelmassa.
