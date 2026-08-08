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
    return webdriver.Remote(APPIUM_URL, options=o)


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
            felder[0].clear(); felder[0].send_keys(prop)
            felder[1].clear(); felder[1].send_keys(user)
            felder[2].clear(); felder[2].send_keys(pw)
            # Der Knopf heißt je nach Sprache "Login" oder "Anmelden"
            knopf = finde(d, 'Login', 5)
            if knopf:
                knopf.click()
            if angemeldet(d, 45):
                return True
        time.sleep(3)
    return angemeldet(d, 10)


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


def testnachrichten_loeschen():
    django(f"from webinterface.models import ChatMessage; ChatMessage.objects.using('property_1').filter(text__contains='{TEST_PREFIX}').delete()")
