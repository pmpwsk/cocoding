# Collaborative Coding Plattform

# Inhaltsverzeichnis

- [Einleitung und allgemeine Projektbeschreibung](#einleitung-und-allgemeine-projektbeschreibung)
- [Projektstatus](#projektstatus)
- [Features](#features)
- [Technologien](#technologien)
- [Installation](#installation)
- [Benutzung in der IDE](#benutzung-in-der-ide)
- [Responsive Design](#responsive-design)
- [Nutzerfreundliches Design](#nutzerfreundliches-design)
- [Entwickler/innen](#entwicklerinnen)
- [Lizenz](#lizenz)
- [Tests](#tests)
- [Ausblick](#ausblick)
- [Fazit](#fazit)

# Einleitung und allgemeine Projektbeschreibung
[Zurück zum Inhaltsverzeichnis](#inhaltsverzeichnis)

Willkommen zur Dokumentation der kollaborativen Coding-Plattform mit Namen cocoding!

Benutzer/innen haben mit cocoding die Möglichkeit, in Echtzeit gemeinsam Code zu schreiben und dabei einen Chat zu benutzen.

Cocoding zeichnet sich durch eine benutzerfreundliche und responsive Oberfläche aus. Es gibt einen Multi-Browser-Support und es können verschiedene mobile Endgeräte benutzt werden.

Programmcode in ausgewählten Programmiersprachen kann dank cocoding unkompliziert gemeinsam erstellt, gespeichert und (teilweise) kompiliert werden. Hervorzuheben ist, dass es eine Download-Funktion gibt, so dass der Programmcode lokal verwendet und überarbeitet werden kann.

# Projektstatus
[Zurück zum Inhaltsverzeichnis](#inhaltsverzeichnis)

Cocoding ist das Ergebnis einer agilen Teamarbeit im Fachpraktikum Computer Supported Cooperative Work (CSCW) im Lehrgebiet Kooperative Systeme an der Fakultät für Mathematik und Informatik an der FernUniversität in Hagen. Der Zeitraum für die Teamarbeit umfasste das Wintersemester 2024/25, eingeteilt in acht Iterationen von jeweils zwei Wochen mit einem Stundenumfang von dreißig Stunden pro Person und Sprint. 
Das Team wurde von den Dozenten Herrn Thomas Kasakowskij, M.A., und Herrn Paul Christ, M.Sc., betreut. 

Andere Teams eines vergleichbaren Fachpraktikums könnten das Projekt weiterentwickeln.

# Features
[Zurück zum Inhaltsverzeichnis](#inhaltsverzeichnis)

Die Plattform bietet folgende Funktionen:
- **Echtzeit-Codierung**: Benutzer/innen können gleichzeitig an einem Code-Dokument arbeiten.
- **Code-Editor**: Im Code-Editor ist es möglich Programmcode in den Sprachen Python und JavaScript zu schreiben, bearbeiten, zu löschen oder zu speichern und zu kompilieren. Andere ausgwählte Programmiersprachen (z. B. CSS und html) können verwendet, jedoch nicht kompiliert werden. Ein Syntax-Highlighting ist auch für diese Programmsprachen vorgesehen. 
- **Syntax-Highlighting**: Unterstützung für JavaScript, Python, CSS und html etc. zur Verbesserung der Lesbarkeit des Codes.
- **Integrierter Chat**: Sowohl synchron, als auch asynchron können Benutzer/innen miteinander chatten. Die Nachrichten können erstellt, versendet, nachträglich bearbeitet und gelöscht werden.
- **Kommentarfunktion**: Benutzer/innen können Programmcode oder Text markieren und als Kommentar in den Chat einfügen und versenden. Auf diese Weise ist der Austausch von Anmerkungen oder das Stellen und Beantworten von Fragen unkompliziert möglich. 
- **Speicherung von Programmcode**: Benutzer/innen können Programmcode in Projekten, Ordnern und Dateien speichern. Somit steht der Programmcode zur weiteren Verarbeitung zur Verfügung. 
- **Download**: Benutzer/innen können den Programmcode zur weiteren lokalen Verarbeitung speichern. 
- **Versionsverwaltung und Versionskontrolle**: Die Historie einer Datei kann über das Speichern von Versionen sichtbar gemacht werden. Das Wiederherstellen von Versionen ermöglicht es, Programmcode zu vergleichen und lösungsorientiert die richtige Version auszuwählen und zu benutzen. 
- **Kompilieren im Browser**: Benutzer/innen können durch den Multi-Browser-Support den Programmcode direkt im jeweiligen Browser kompilieren und auf Grund möglicher Fehlermeldungen den Programmcode bearbeiten oder die Ergebnisse anschauen.
- **Einrückung von Zeilen**: Um den Konventionen eines guten Programmierstils zu genügen ist, ist die Einrückung von Zeilen möglich. Die Anzahl der Einrückungen kann eigenständig bestimmt werden.
- **Dashboard**: Das Dashboard verfügt zur intuitiven Benutzung über ein Dropdown-Menü. Über die grafische Benutzeroberfläche des Dashboards können Benutzer/innen wählen, ob sie den Code-Editor benutzen oder Scrum Poker spielen möchten. Das Scrum-Poker-Spiel diente dem Team bei der Entwicklung dazu, gemeinsam über den Zeitaufwand der anfallenden Aufgaben einen Konsens zu bilden.
- **Projektverwaltung**: Über die Projektverwaltung ist es Benutzer/innen möglich, Projekte, sowie die dazugehörigen Dateien und Ordner zu erstellen, zu benennen, zu speichern, zu sortieren oder zu löschen. Ein Anpinnen ist möglich, um neben der alphabetischen Übersicht, selbst eine Reihenfolge der Projekte festzulegen und unkompliziert auf häufig verwendete oder aktuelle Projekte zugreifen zu können. Alle Funktionen werden durch Dialoge oder (Lösch-)Warnungen begleitet, so dass nichts aus Versehen verändert werden kann. Durch verschiedene Icons sind Projekte, Ordner und Dateien von einander unterscheidbar, so dass die Übersichtlichkeit bei der Benutzung der Anwendung gewährleistet ist.  
- **Zusammenarbeit in Projekten**: Benutzer/innen können andere Benutzer/innen zur Zusammenarbeit in Projekten, Ordnern und Dateien einladen und kooperativ zusammenarbeiten. Es besteht die Möglichkeit in Benutzerlisten zu suchen.
- **Dark Mode**: Benutzer/innen können selbst das Farbschema wählen, dass sie bei der Benutzung von cocoding verwenden möchten. 
- **Authentifizierung**: Durch die Verwendung eines Namens für eine/n Benutzer/in kann die Identität technisch zugeordnet werden. 
- **Account löschen**: Es ist möglich ein einzelnes Projekt zu verlassen. Ebenso ist es möglich, die Anwendung als Ganze zu verlassen und den eigenen Account zu löschen. Der gelöschte Benutzer bzw. die gelöschte Benutzerin wird mit einem Vermerk in den beteiligten Dateien angezeigt.          
- **Lokalzeit**: Als Zeitzone wird in der gesamten Anwendung die mitteleuropäische Zeit (MEZ) verwendet. Jedem User/ jeder Userin wird jedoch die jeweilige Lokalzeit angezeigt.   
- **Sidebar**: Es gibt für die Desktop-Verwendung eine Sidebar mit verschiedenen Icons, die eine leichte Handhabung aller Funktionalitäten ermöglicht. Unter anderem ist ein Dateibaum aufrufbar, der den Zusammenhang der Projekte, Ordner und Dateien anschaulich visualisiert.    
- **Easteregg**: Benutzer/innen können die Methode console.cocoding() kompilieren und staunen, welche Konsolen-Ausgabe sichtbar wird.

# Technologien
[Zurück zum Inhaltsverzeichnis](#inhaltsverzeichnis)

Folgende Technologien wurden für die Entwicklung der Plattform verwendet:  
**Frontend:** 
- Blazor (Microsofts Single-Page-App-Framework)   
- Für den Code-Editor wurde https://codemirror.net/ in der Version 6 benutzt. 

**Backend:** 
- ASP.NET 

**Datenbank**: 
- Als Datenbank wurde das Allzweck-Open-Source-Managementsystem für relationale Datenbanken mit dem Namen MariaDB verwendet. 

**Server:** 
- Hauptinstanz: Debian 12
- Eine eigene Instanz ist möglich. 

# Installation
[Zurück zum Inhaltsverzeichnis](#inhaltsverzeichnis)

1. .NET 9 installieren
1. MySQL-kompatible Datenbank wie MariaDB installieren und wie [hier](https://catalpa-gitlab.fernuni-hagen.de/ks/fapra/fachpraktikum-2024/alpha/collaborative-coding-plattform/-/wikis/Datenbank) einrichten
1. Repo herunterladen und bauen, oder [hier](https://files.uwap.org/@cocoding/) einen Build herunterladen
1. Build an einem beliebigen Ort ablegen
1. Konfigurationsdatei wie [hier](https://catalpa-gitlab.fernuni-hagen.de/ks/fapra/fachpraktikum-2024/alpha/collaborative-coding-plattform/-/wikis/Konfigurationsdatei) erstellen
1. Ggf. die konfigurierten Ports in der Firewall erlauben
1. Automatischen Start konfigurieren, z.B. per Service-Datei
1. Programm starten
1. Neues Konto registrieren
1. Entsprechend der Dokumentation in `cocoding help` mit `cocoding make-admin mein.nutzername` den Nutzer (z.B. mit Nutzername "mein.nutzername" zum Administrator machen)
1. Regelmäßig unter "Burgermenü > Administration > Server" nachsehen, ob es ein Update gibt, und dieses dort durchführen (aktuell gibt es so nur Builds für Linux ARM64 und die Installation von Updates erfordert einen Start über [Wrapper](https://uwap.org/wrapper))

# Benutzung in der IDE
[Zurück zum Inhaltsverzeichnis](#inhaltsverzeichnis)

- .NET 9 installieren (Überprüfung mit Konsolenbefehl dotnet --info) 
- Das Repository [hier](https://catalpa-gitlab.fernuni-hagen.de/ks/fapra/fachpraktikum-2024/alpha/collaborative-coding-plattform) klonen
- Verzeichnis anlegen und in das Verzeichnis wechseln 
- Konsolenbefehle: dotnet run oder dotnet watch
- Browser-Aufruf: http://localhost:5000/
- Gitbefehle:<br/>
git remote add origin https://catalpa-gitlab.fernuni-hagen.de/ks/fapra/fachpraktikum-2024/alpha/collaborative-coding-plattform.git <br/>
git branch -M main<br/>
git push -uf origin main<br/>

# Responsive Design
[Zurück zum Inhaltsverzeichnis](#inhaltsverzeichnis)

Die Benutzer/innen können die Browseranwendung cocoding auf dem Desktop, auf einem Smartphone oder einem Tablet verwenden. Als CSS-Framework sorgt Bootstrap dafür, dass die Benutzeroberfläche sich dem Gerät anpasst und die Benutzerfreundlichkeit sichergestellt ist. Für die Oberfläche wurde eine Kombination aus Bootstrap und eigener CSS-Styles genutzt.   

# Nutzerfreundliches Design
[Zurück zum Inhaltsverzeichnis](#inhaltsverzeichnis)

Die Benutzer/innen erwartet ein intuitiv gestaltetes Design. Die Beschriftung der Buttons ist zwar in deutscher Sprache, aber durch die verwendeten Symbole ist es möglich, Sprachbarrieren zu überbrücken. Die Hauptfunktionalitäten der Plattform können so unabhängig von Kenntnissen der deutschen Sprache verwendet werden.   

# Entwickler/innen
[Zurück zum Inhaltsverzeichnis](#inhaltsverzeichnis)

- Dennis Erdmann
- Judith Göd  
- Florian Pompowski
- Bernd Reiß
- Ole Stollreiter

# Lizenz
[Zurück zum Inhaltsverzeichnis](#inhaltsverzeichnis)

Cocoding nutzt die MIT-Lizenz. 
Informationen finden Sie [hier](https://opensource.org/license/mit).

# Tests
[Zurück zum Inhaltsverzeichnis](#inhaltsverzeichnis)

Informationen zu unseren Tests finden Sie [hier](https://catalpa-gitlab.fernuni-hagen.de/ks/fapra/fachpraktikum-2024/alpha/collaborative-coding-plattform/-/wikis/Tests). 

# Ausblick
[Zurück zum Inhaltsverzeichnis](#inhaltsverzeichnis)

Wir haben zahlreiche zukünftige Features im Sinn. 
Als Auswahl nennen wir Folgende: 

- Kompilierung weiterer Programmiersprachen
- Privates chatten zwischen zwei Personen realisieren
- Projektmanagement-Funktionen (Projektkalender, Terminverfolgung, Aufgaben-Zuweisung inkl. Labels)
- Emoji-Bibliothek für den Chat 

# Fazit
[Zurück zum Inhaltsverzeichnis](#inhaltsverzeichnis)

Cocoding als kollaborative Coding-Plattform bietet Benutzer/innen eine Lösung für Entwickler/innen, die in Echtzeit gemeinsam Code schreiben, speichern und ausführen möchten.  
Durch die Benutzerfreundlichkeit und den Chat wird die Produktivität und Zusammenarbeit sowohl bei synchronen als auch bei asynchronen Projekten gesteigert.  

