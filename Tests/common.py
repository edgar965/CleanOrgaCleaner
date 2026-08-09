# -*- coding: utf-8 -*-
"""Gemeinsame Helfer für alle Appium-Testsuiten von CleanOrgaCleaner.

Voraussetzungen (siehe README.md):
- Android-Emulator läuft (pixel_7_-_api_36, emulator-5554)
- Appium-Server auf 127.0.0.1:4723 mit UiAutomator2-Treiber
- Debug-APK MIT eingebetteten Assemblies installiert
  (dotnet build -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true)
- SSH-Zugang zu root@91.99.235.72 für Server-Verifikation (property_1)

Testzugang: Property 1, User tom / Passwort tom (Cleaner id 9). Admin id 11.
"""
import atexit
import os
import subprocess
import time

ADB = r'C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe'

# Zielgeraet: Emulator (Standard) oder ein angeschlossenes Telefon.
# Ueberschreibbar per Umgebungsvariable, z. B.
#   $env:CLEANORGA_GERAET = "N550000000000126920"
GERAET = os.environ.get('CLEANORGA_GERAET', 'emulator-5554')

PAKET = 'com.cleanorga.cleaner'
ACTIVITY = 'crc64872e68f2eafd0b30.MainActivity'
APPIUM_URL = 'http://127.0.0.1:4723'
SSH_ZIEL = 'root@91.99.235.72'
TEST_PREFIX = '[APPIUM-TEST]'

from appium import webdriver
from appium.options.android import UiAutomator2Options
from appium.webdriver.common.appiumby import AppiumBy

from begriffe import Begriffe


# ---------------------------------------------------------------- adb / Netz
def adb(*args):
    return subprocess.run([ADB, '-s', GERAET, *args],
                          capture_output=True, text=True, timeout=60).stdout.strip()


def netz(an: bool):
    z = 'enable' if an else 'disable'
    adb('shell', 'svc', 'wifi', z)
    adb('shell', 'svc', 'data', z)
    time.sleep(3)


def app_laeuft() -> bool:
    if adb('shell', 'pidof', PAKET).strip():
        return True
    return PAKET in adb('shell', 'ps', '-A')


def app_neustart():
    adb('shell', 'am', 'force-stop', PAKET)
    time.sleep(1)
    adb('shell', 'am', 'start', '-n', f'{PAKET}/{ACTIVITY}')
    ende = time.time() + 25
    while time.time() < ende and not app_laeuft():
        time.sleep(1)
    time.sleep(3)


def home():
    adb('shell', 'input', 'keyevent', 'KEYCODE_HOME')
    time.sleep(3)


def kein_fatal() -> bool:
    log = adb('shell', 'logcat', '-d', '-b', 'crash')
    return not ('FATAL EXCEPTION' in log and PAKET in log)


# ---------------------------------------------------------------- Server (SSH)
def ssh(befehl: str) -> str:
    return subprocess.run(['ssh', SSH_ZIEL, befehl], capture_output=True, text=True, timeout=90).stdout.strip()


def django(code: str) -> str:
    """Django-ORM-Einzeiler auf dem Server (property_1)."""
    script = (
        "import os, django; os.environ.setdefault('DJANGO_SETTINGS_MODULE','CleanOrga.settings.prod_settings'); "
        "django.setup(); from webinterface.db_router import set_current_property; set_current_property(1); " + code
    )
    return ssh(
        "cd /var/www/cleanorga && set -a && . ./.env && set +a && "
        f"DJANGO_SETTINGS_MODULE=CleanOrga.settings.prod_settings PYTHONPATH=/var/www/cleanorga venv/bin/python -c \"{script}\" 2>/dev/null"
    )


# ---------------------------------------------------------------- Appium
def treiber():
    o = UiAutomator2Options()
    o.platform_name = 'Android'
    o.device_name = GERAET
    o.app_package = PAKET
    o.app_activity = ACTIVITY
    o.no_reset = True
    o.auto_grant_permissions = True
    o.new_command_timeout = 300

    tastatur_ohne_autokorrektur()
    return webdriver.Remote(APPIUM_URL, options=o)


# ------------------------------------------------- Tastatur ohne Autokorrektur
APPIUM_IME = 'io.appium.settings/.UnicodeIME'
_ime_vorher = None


def tastatur_ohne_autokorrektur():
    """Auf Appiums Tastatur umschalten und die bisherige merken.

    Die Google-Tastatur korrigiert Eingaben: Aus dem Benutzernamen "tom" wurde
    beim Verlassen des Feldes "Tom " (Grossbuchstabe plus Leerzeichen), worauf
    die Anmeldung mit "Benutzername oder Passwort falsch" abgewiesen wurde.
    Die Capabilities unicode_keyboard/reset_keyboard haben daran nichts
    geändert - erst das Umschalten per adb wirkt.
    """
    global _ime_vorher
    aktuell = adb('shell', 'settings', 'get', 'secure', 'default_input_method').strip()
    if aktuell.startswith('io.appium'):
        return
    _ime_vorher = aktuell
    adb('shell', 'ime', 'enable', APPIUM_IME)
    adb('shell', 'ime', 'set', APPIUM_IME)
    time.sleep(1)


def tastatur_zuruecksetzen():
    """Ursprüngliche Tastatur wiederherstellen (läuft automatisch am Ende)."""
    global _ime_vorher
    if not _ime_vorher:
        return
    adb('shell', 'ime', 'set', _ime_vorher)
    _ime_vorher = None


atexit.register(tastatur_zuruecksetzen)


def finde(d, text, timeout=15):
    """Element mit diesem Text suchen - in JEDER Sprache der App.

    Sucht nicht nur den übergebenen Text, sondern alle gleichbedeutenden
    Beschriftungen (siehe begriffe.Begriffe). Ohne das hing das Ergebnis an
    der zufällig eingestellten Gerätesprache.
    """
    varianten = Begriffe.varianten(text)
    ende = time.time() + timeout
    while time.time() < ende:
        for wort in varianten:
            try:
                return d.find_element(AppiumBy.ANDROID_UIAUTOMATOR,
                                      f'new UiSelector().textContains("{wort}")')
            except Exception:
                pass
        time.sleep(1)
    return None


def finde_desc(d, text, timeout=8):
    varianten = Begriffe.varianten(text)
    ende = time.time() + timeout
    while time.time() < ende:
        for wort in varianten:
            try:
                return d.find_element(AppiumBy.ANDROID_UIAUTOMATOR,
                                      f'new UiSelector().descriptionContains("{wort}")')
            except Exception:
                pass
        time.sleep(1)
    return None


def edittexts(d):
    return d.find_elements(AppiumBy.CLASS_NAME, 'android.widget.EditText')


def sende_text(d, text) -> bool:
    """Text ins Chat-Eingabefeld schreiben und abschicken.

    Bewusst OHNE vorherigen Klick ins Feld: Der Klick öffnet die Tastatur,
    die den Senden-Knopf verdeckt - der Klick darauf ging dann ins Leere
    (Testfall RT02 meldete deshalb dauerhaft "kam nicht am Server an",
    obwohl derselbe Vorgang in CH02 lief).
    """
    felder = edittexts(d)
    if not felder:
        return False

    feld = felder[-1]
    feld.send_keys(text)
    time.sleep(1)

    # Bewusst NICHT nach "Send" suchen: Die Enter-Taste der Bildschirmtastatur
    # heisst ebenfalls so, und ein Tap darauf schickt in der App nichts ab.
    # Der Sende-Knopf ist der Knopf rechts in der Eingabezeile.
    senden = _sendeknopf_neben(d, feld)
    if senden is None:
        return False

    tippe_auf(d, senden)
    time.sleep(4)
    return True


def tastatur_schliessen(d):
    """Bildschirmtastatur einklappen, falls eine sichtbar ist.

    Bewusst OHNE Zurück-Taste als Notweg: Appiums Tastatur zeigt keine
    Oberfläche, meldet dem System aber trotzdem "eingeblendet".
    hide_keyboard() scheitert dann - und ein Zurück-Tastendruck schliesst
    keine Tastatur, sondern verlässt die App (Anmeldung landete so auf dem
    Startbildschirm des Geräts).
    """
    try:
        d.hide_keyboard()
        time.sleep(1)
    except Exception:
        pass


def tippe_auf(d, element):
    """Element über seine Mitte antippen statt per click().

    Auf den MAUI-Oberflächen kommt ``element.click()`` nicht zuverlässig an -
    beim Senden im Chat blieb der Text im Feld stehen, während derselbe Punkt
    per Koordinaten-Tap sofort ausgelöst hat (nachgewiesen am 09.08.2026:
    ``adb shell input tap`` legte die Nachricht an, click() nicht).
    """
    lage = element.location
    groesse = element.size
    d.tap([(lage['x'] + groesse['width'] // 2,
            lage['y'] + groesse['height'] // 2)])


def _sendeknopf_neben(d, feld):
    """Der Knopf rechts neben dem Eingabefeld (gleiche Zeile).

    Der Sende-Knopf trägt nur ein Symbol, keine Beschriftung. Die frühere
    Notlösung "der letzte Knopf auf dem Bildschirm" griff daneben, sobald die
    Tastatur offen war - deren Tasten sind ebenfalls Buttons und stehen in der
    Liste weiter hinten. Der Text blieb dann im Feld stehen und kam nie an.
    """
    zeile_y = feld.location['y']
    knoepfe = [b for b in d.find_elements(AppiumBy.CLASS_NAME, 'android.widget.Button')
               if abs(b.location['y'] - zeile_y) <= 80]
    if not knoepfe:
        return None
    return max(knoepfe, key=lambda b: b.location['x'])


def scrolle_zu(d, text, versuche=4):
    """Nach unten wischen, bis der Text sichtbar ist. Liefert das Element.

    Android führt Elemente eines Scroll-Bereichs erst in der Hierarchie, wenn
    sie gezeichnet wurden: Der Mitteilungen-Abschnitt der Einstellungen war
    ohne Wischen schlicht nicht auffindbar - und ein Test, der einfach "den
    letzten Schalter" nahm, erwischte stattdessen den Biometrie-Schalter.
    """
    gefunden = finde(d, text, 2)
    if gefunden is not None:
        return gefunden

    groesse = d.get_window_size()
    x = groesse['width'] // 2
    von_y = int(groesse['height'] * 0.75)
    bis_y = int(groesse['height'] * 0.25)

    for _ in range(versuche):
        d.swipe(x, von_y, x, bis_y, 400)
        time.sleep(1)
        gefunden = finde(d, text, 2)
        if gefunden is not None:
            return gefunden
    return None


def schalter_neben(d, text):
    """Der Schalter, der in derselben Zeile wie ``text`` steht.

    Zuordnung über die senkrechte Lage - zuverlässiger als "der letzte
    Schalter auf dem Bildschirm", der von der Scroll-Position abhängt.
    """
    beschriftung = scrolle_zu(d, text)
    if beschriftung is None:
        return None

    ziel_y = beschriftung.location['y']
    schalter = d.find_elements(AppiumBy.CLASS_NAME, 'android.widget.Switch')
    if not schalter:
        return None

    return min(schalter, key=lambda s: abs(s.location['y'] - ziel_y))


def oeffne_menue(d):
    """Hamburger-Menü im Header öffnen (App navigiert über Flyout-Overlay,
    nicht über eine untere Tab-Bar).

    Zuerst das echte ☰-Element anklicken; der blinde Tap auf 56%/14% war auf
    die Emulator-Auflösung kalibriert und landet auf anderen Geräten leicht
    auf dem Nachbarknopf "Start".
    """
    try:
        d.find_element(AppiumBy.ANDROID_UIAUTOMATOR,
                       'new UiSelector().text("☰")').click()
        time.sleep(2)
        return
    except Exception:
        pass
    g = d.get_window_size()
    d.tap([(int(g['width'] * 0.56), int(g['height'] * 0.14))])
    time.sleep(2)


def navigiere(d, menuepunkt) -> bool:
    """Über das Hamburger-Menü zu Today/Chat/Auftrag/Settings navigieren."""
    ziel = finde(d, menuepunkt, 3)
    if ziel is None:
        oeffne_menue(d)
        ziel = finde(d, menuepunkt, 5)
    if ziel is not None:
        ziel.click()
        time.sleep(4)
        return True
    return False


def _knopf_mit_text(d, beschriftung):
    try:
        return d.find_element(
            AppiumBy.ANDROID_UIAUTOMATOR,
            f'new UiSelector().className("android.widget.Button").text("{beschriftung}")')
    except Exception:
        return None


def systemdialog_wegklicken(d):
    """Nachfragen abweisen, die nach dem Anmelden über der App liegen.

    Zwei Stück tauchen dort auf und liessen jeden Test die Anmeldung als
    gescheitert werten, obwohl sie geklappt hatte:
      * der Google-Passwortmanager ("Passwort speichern?")
      * die App selbst ("Fingerabdruck für zukünftige Anmeldungen aktivieren?")

    Das "Nein" des Fingerabdruck-Dialogs wird bewusst nur gedrückt, wenn
    dieser Dialog auch wirklich zu sehen ist - sonst würde es andere
    Ja/Nein-Nachfragen der App mit abräumen (etwa "Arbeitszeit beenden?").
    """
    for beschriftung in ('Nicht jetzt', 'Not now', 'Never', 'Nie'):
        knopf = _knopf_mit_text(d, beschriftung)
        if knopf is not None:
            tippe_auf(d, knopf)
            time.sleep(1.5)
            return True

    if finde(d, 'Fingerabdruck', 1) is not None or finde(d, 'Fingerprint', 1) is not None:
        for beschriftung in ('NEIN', 'Nein', 'NO', 'No'):
            knopf = _knopf_mit_text(d, beschriftung)
            if knopf is not None:
                tippe_auf(d, knopf)
                time.sleep(1.5)
                return True
    return False


def angemeldet(d, timeout=30) -> bool:
    """Wartet, bis die Hauptoberfläche steht.

    Aktiv warten statt fester Pause: Auf einem langsamen Telefon brauchte die
    Anmeldung länger als die früheren 10+8 Sekunden, worauf der Test einen
    Fehlschlag meldete, obwohl die App gleich darauf einwandfrei lief.
    """
    ende = time.time() + timeout
    while time.time() < ende:
        if finde(d, 'Today', 2) is not None or finde(d, 'Chat', 1) is not None:
            return True
        systemdialog_wegklicken(d)
    return False


def oeffne_chat_mit(d, name) -> bool:
    """Gesprächsverlauf mit einem Kontakt öffnen.

    Wichtig: Der Verlauf öffnet sich über den 'Chat'-Knopf in der Zeile, NICHT
    über den Namen. Ein Klick auf den Namen lässt die App wirkungslos in der
    Liste stehen - dort gibt es kein Eingabefeld, weshalb ein anschliessendes
    Senden ins Leere lief.
    """
    navigiere(d, 'Chat')
    if finde(d, name, 6) is None:
        return False

    try:
        if name == 'Admin':
            knopf = d.find_element(
                AppiumBy.ANDROID_UIAUTOMATOR,
                'new UiSelector().className("android.widget.Button")'
                '.textContains("Chat").instance(0)')
        else:
            knopf = d.find_element(
                AppiumBy.ANDROID_UIAUTOMATOR,
                'new UiSelector().className("android.widget.Button")'
                f'.fromParent(new UiSelector().textContains("{name}"))')
        knopf.click()
        time.sleep(4)
        return True
    except Exception:
        return False


def login(d, prop='1', user='tom', pw='tom') -> bool:
    """Robuster Login mit Retry (nach Netzwechsel/Neustart flaky)."""
    for _ in range(3):
        # Beim Start entscheidet sich erst nach ein paar Sekunden, ob die
        # gespeicherte Anmeldung greift oder die Anmeldemaske kommt.
        if angemeldet(d, 12):
            return True
        felder = []
        ende = time.time() + 20
        while time.time() < ende:
            felder = edittexts(d)
            if len(felder) >= 3:
                break
            time.sleep(1)
        if len(felder) >= 3:
            _fuelle_feld(d, felder[0], prop)
            _fuelle_feld(d, felder[1], user)
            _fuelle_feld(d, felder[2], pw)

            # Tastatur zu: Sie verdeckt den Anmelden-Knopf so weit, dass er
            # nicht einmal mehr in der Element-Hierarchie auftaucht - die
            # Anmeldung schlug deshalb fehl, obwohl alle Felder korrekt
            # ausgefüllt waren.
            tastatur_schliessen(d)

            # Der Knopf heißt je nach Sprache "Login" oder "Anmelden"
            knopf = finde(d, 'Login', 5)
            if knopf:
                tippe_auf(d, knopf)
            if angemeldet(d, 45):
                return True
        time.sleep(3)
    return angemeldet(d, 10)


def _fuelle_feld(d, feld, wert):
    """Feld mit genau diesem Wert belegen.

    Bevorzugt ``set_value``: Das schreibt den Wert direkt in das Feld und geht
    an der Bildschirmtastatur vorbei. Deren Autokorrektur machte aus dem
    Benutzernamen "tom" beim Verlassen des Feldes ein "Tom " - Grossbuchstabe
    plus angehängtes Leerzeichen -, worauf die Anmeldung mit "Benutzername
    oder Passwort falsch" abgewiesen wurde.
    """
    try:
        feld.clear()
    except Exception:
        pass

    try:
        feld.set_value(wert)
        time.sleep(0.3)
        return
    except Exception:
        pass

    # Notweg, falls set_value nicht unterstützt wird
    tippe_auf(d, feld)
    time.sleep(0.5)
    feld.send_keys(wert)
    time.sleep(0.5)


def screenshot(d, name):
    """Legt den Screenshot neben dieser Datei ab - der Pfad war frueher fest
    auf D:\\Daten verdrahtet und lief auf anderen Rechnern ins Leere."""
    try:
        ordner = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'screenshots')
        os.makedirs(ordner, exist_ok=True)
        d.get_screenshot_as_file(os.path.join(ordner, f'{name}.png'))
    except Exception:
        pass


# ---------------------------------------------------------------- Protokoll
class Protokoll:
    def __init__(self):
        self.zeilen = []

    def __call__(self, nr, titel, ok, detail=''):
        self.zeilen.append((nr, titel, bool(ok), detail))
        print(f'[{"PASS" if ok else "FAIL"}] {nr}: {titel}' + (f' - {detail}' if detail else ''), flush=True)

    def abschluss(self) -> int:
        print('\n=== ERGEBNIS ===', flush=True)
        b = sum(1 for _, _, ok, _ in self.zeilen if ok)
        for nr, t, ok, det in self.zeilen:
            print(f'{"PASS" if ok else "FAIL"}  {nr:10} {t}' + (f' [{det}]' if det else ''), flush=True)
        print(f'{b}/{len(self.zeilen)} bestanden', flush=True)
        return 0 if b == len(self.zeilen) else 1


def warte_auf_server(pruefung, timeout=60, takt=5):
    """Wartet, bis ``pruefung()`` wahr wird - für Vorgänge über die Leitung.

    Feste Pausen sind hier untauglich: Eine offline abgesetzte Nachricht
    braucht nach dem Wiederverbinden mal 15, mal 30 Sekunden, bis die
    Warteschlange sie losgeschickt hat. Ein starres ``sleep(25)`` meldete
    deshalb sporadisch "kam nicht an", obwohl sie kurz darauf da war.
    """
    ende = time.time() + timeout
    while time.time() < ende:
        if pruefung():
            return True
        time.sleep(takt)
    return pruefung()


def testnachrichten_loeschen():
    django(f"from webinterface.models import ChatMessage; ChatMessage.objects.using('property_1').filter(text__contains='{TEST_PREFIX}').delete()")
