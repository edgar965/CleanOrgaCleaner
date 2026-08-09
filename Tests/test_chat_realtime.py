# -*- coding: utf-8 -*-
"""Echtzeit-Chat-Test (Vordergrund / WebSocket) fuer CleanOrgaCleaner am Emulator.

RT01: eine serverseitig als Admin injizierte Nachricht an tom erscheint LIVE
      (WebSocket-Push, ohne App-Neustart).
RT02: tom sendet an Admin -> landet am Server (DB-Count steigt).

Voraussetzungen siehe common.py (Emulator + Appium + aktueller Debug-APK).
Aufruf: python test_chat_realtime.py
"""
import sys
import time
from common import (treiber, finde, django, screenshot, sende_text,
                    oeffne_chat_mit, adb, login, PAKET, ACTIVITY, TEST_PREFIX,
                    Protokoll)

# Die Verwaltung hat bewusst KEINE Cleaner-Id - siehe injiziere().
TOM_ID = 9


def app_bereit(d, timeout=40) -> bool:
    ende = time.time() + timeout
    while time.time() < ende:
        if (finde(d, 'Today', 2) is not None or finde(d, 'Start', 2) is not None
                or finde(d, 'Chat', 2) is not None):
            return True
        time.sleep(2)
    return False


def oeffne_admin(d) -> bool:
    """Verlauf mit der Verwaltung öffnen (gemeinsame Fassung in common.py).

    Die frühere Fassung tippte auf eine feste Koordinate und fiel notfalls auf
    einen Klick auf den Namen zurück - der öffnet den Verlauf aber nicht. Die
    App blieb in der Liste stehen, und RT02 fand kein Eingabefeld.
    """
    return oeffne_chat_mit(d, 'Admin')


def injiziere(text) -> str:
    """Nachricht der Verwaltung an tom - auf demselben Weg wie die Oberfläche.

    ``sender=None`` kennzeichnet die Verwaltung (siehe
    ``ChatMessage.get_admin_conversation``); das Verschicken übernimmt
    ``broadcast_chat_message``, damit Nutzlast und Empfängerwahl zum
    Produktivcode passen.
    """
    code = (
        "from webinterface.models import ChatMessage, Cleaner; "
        "from webinterface.views.shared.websocket import broadcast_chat_message; "
        f"t=Cleaner.objects.using('property_1').get(id={TOM_ID}); "
        f"m=ChatMessage.objects.using('property_1').create(sender=None, receiver=t, text='{text}'); "
        "broadcast_chat_message(t.id, m); "
        "print(m.id)"
    )
    return django(code).strip()


def db_count(teil) -> int:
    r = django("from webinterface.models import ChatMessage; "
               f"print(ChatMessage.objects.using('property_1').filter(text__contains='{teil}').count())")
    return int(r) if r.strip().isdigit() else -1


def main():
    log = Protokoll()
    django(f"from webinterface.models import ChatMessage; ChatMessage.objects.using('property_1').filter(text__contains='{TEST_PREFIX}').delete()")
    d = treiber()
    try:
        adb('shell', 'am', 'force-stop', PAKET)
        adb('shell', 'am', 'start', '-n', f'{PAKET}/{ACTIVITY}')
        time.sleep(6)
        # Nach Neuinstallation ist die Session weg -> Login-Screen. Sonst schon eingeloggt.
        bereit = app_bereit(d, 15)
        if not bereit:
            bereit = login(d)          # prop=1, tom/tom
        log('T-READY', 'App bereit / eingeloggt (tom)', bereit)

        if not oeffne_admin(d):
            log('RT01', 'Admin-Chat oeffnen', False)
            log('RT02', 'Admin-Chat oeffnen', False, 'uebersprungen')
            return log.abschluss()

        time.sleep(4)  # WebSocket verbinden lassen

        # RT01: serverseitig injizierte Nachricht muss LIVE erscheinen
        txt = f'{TEST_PREFIX} rt01-live'
        mid = injiziere(txt)
        gefunden = False
        for _ in range(10):
            if finde(d, 'rt01-live', 2) is not None:
                gefunden = True
                break
            time.sleep(1)
        screenshot(d, 'rt01_live')
        log('RT01', f'Injizierte Admin-Nachricht live per WS (msg {mid})', gefunden)

        # RT02: tom sendet an Admin.
        # Den Rueckgabewert auswerten: Sonst ist bei DB-count=0 nicht zu
        # unterscheiden, ob das Senden scheiterte oder die Nachricht nur noch
        # nicht am Server war.
        gesendet = sende_text(d, f'{TEST_PREFIX} rt02-send')
        time.sleep(3)
        cnt = db_count('rt02-send')
        screenshot(d, 'rt02_send')
        log('RT02', 'tom sendet an Admin -> am Server', cnt >= 1,
            f'abgeschickt={gesendet}, DB-count={cnt}')

        return log.abschluss()
    finally:
        django(f"from webinterface.models import ChatMessage; ChatMessage.objects.using('property_1').filter(text__contains='{TEST_PREFIX}').delete()")
        try:
            d.quit()
        except Exception:
            pass


if __name__ == '__main__':
    sys.exit(main())
