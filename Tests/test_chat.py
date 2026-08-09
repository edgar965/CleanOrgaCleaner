# -*- coding: utf-8 -*-
"""Automatisierte Chat-Tests für CleanOrgaCleaner (Android-Emulator + Appium).

Deckt die Fälle aus chat_testcases.md ab. Testzugang: Property 1 / tom / tom (id 9).
Kollege für Kollegen-Tests: aylin (id 3). Admin: id 11.

Server-Injektion (eingehende Nachricht an tom) über den Django-Channel-Layer,
gleicher Pfad wie ein echter Admin->Cleaner-Chat (Gruppe cleaner_9__p1).
Server-Verifikation + Aufräumen per SSH (Django-ORM, property_1).
"""
import sys
import time

from common import (ACTIVITY, PAKET, TEST_PREFIX, adb, app_laeuft, django,
                    finde, login, navigiere, netz, oeffne_chat_mit,
                    screenshot, sende_text, treiber, Protokoll)

TOM_ID = 9
AYLIN_ID = 3
# Die Verwaltung hat bewusst KEINE Cleaner-Id: Ihre Nachrichten tragen
# sender=None (siehe injiziere_admin_nachricht).  Ein frueheres ADMIN_ID = 11
# fuehrte genau in die Falle, Admin-Nachrichten als Cleaner anzulegen.

_protokoll = Protokoll()


def injiziere_admin_nachricht(text: str) -> str:
    """Erzeugt eine Nachricht der Verwaltung an tom - auf demselben Weg wie
    die Oberfläche. Gibt die Message-ID zurück.

    Entscheidend ist ``sender=None``: Daran - und NICHT an einem Cleaner mit
    Admin-Rolle - erkennt der Server eine Nachricht der Verwaltung. Sowohl
    ``ChatMessage.get_admin_conversation`` (``sender IS NULL``) als auch
    ``broadcast_chat_message`` fragen genau das ab.

    Die frühere Fassung legte die Nachricht mit ``sender=Cleaner(id=11)`` an
    und baute die WebSocket-Meldung von Hand nach. Ergebnis: Sie kam zwar
    live an, gehörte serverseitig aber in KEINEN Verlauf und wurde deshalb
    nie nachgeladen - CH10 schlug dauerhaft fehl, ohne dass die App einen
    Fehler hatte. Jetzt übernimmt ``broadcast_chat_message`` das Verschicken,
    sodass Nutzlast und Empfängerwahl immer zum Produktivcode passen.
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


def protokoll(nr, titel, ok, detail=''):
    """Ergebniszeile - die Sammlung liegt in der Protokoll-Klasse (common)."""
    _protokoll(nr, titel, ok, detail)


def db_count(text_teil):
    r = django(f"from webinterface.models import ChatMessage; print(ChatMessage.objects.using('property_1').filter(text__contains='{text_teil}').count())")
    return int(r) if r.strip().isdigit() else -1


def main():
    print('=== Chat-Testsuite CleanOrgaCleaner ===', flush=True)
    netz(True)
    django(f"from webinterface.models import ChatMessage; ChatMessage.objects.using('property_1').filter(text__contains='{TEST_PREFIX}').delete()")

    d = treiber()
    try:
        adb('shell', 'am', 'force-stop', PAKET)
        adb('shell', 'am', 'start', '-n', f'{PAKET}/{ACTIVITY}'); time.sleep(6)
        ok = login(d)
        protokoll('T-LOGIN', 'Login tom/tom', ok)

        # CH01: Chat-Liste lädt
        navigiere(d, 'Chat')
        liste_ok = finde(d, 'Admin', 8) is not None
        protokoll('CH01', 'Chat-Liste lädt (Admin sichtbar)', liste_ok)
        screenshot(d, 'ch01')

        # CH02: Nachricht an Admin senden -> DB
        if oeffne_chat_mit(d, 'Admin'):
            txt = f'{TEST_PREFIX} an-admin-ch02'
            sende_text(d, txt)
            time.sleep(3)
            cnt = db_count('an-admin-ch02')
            protokoll('CH02', f'Nachricht an Admin am Server (count={cnt})', cnt >= 1)
            # CH03: erscheint im Verlauf
            im_verlauf = finde(d, 'an-admin-ch02', 5) is not None
            protokoll('CH03', 'Gesendete Nachricht erscheint im Verlauf', im_verlauf)
            screenshot(d, 'ch02')
        else:
            protokoll('CH02', 'Nachricht an Admin', False, 'Admin-Chat nicht gefunden')
            protokoll('CH03', 'Verlauf', False, 'übersprungen')

        # CH04: eingehende Nachricht in Echtzeit (Vordergrund), Admin-Chat offen
        mid = injiziere_admin_nachricht(f'{TEST_PREFIX} echtzeit-ch04')
        time.sleep(6)
        echtzeit = finde(d, 'echtzeit-ch04', 8) is not None
        protokoll('CH04', f'Eingehende Admin-Nachricht in Echtzeit sichtbar (msg {mid})', echtzeit)
        screenshot(d, 'ch04')

        # CH10: App im Hintergrund -> Nachricht injizieren -> zurück -> nachgeladen
        adb('shell', 'input', 'keyevent', 'KEYCODE_HOME'); time.sleep(4)
        injiziere_admin_nachricht(f'{TEST_PREFIX} hintergrund-ch10')
        time.sleep(5)
        adb('shell', 'am', 'start', '-n', f'{PAKET}/{ACTIVITY}'); time.sleep(7)
        # ggf. zurück in den Admin-Chat
        oeffne_chat_mit(d, 'Admin')
        nachgeladen = finde(d, 'hintergrund-ch10', 10) is not None
        protokoll('CH10', 'Im Hintergrund verpasste Nachricht wird beim Öffnen nachgeladen', nachgeladen)
        screenshot(d, 'ch10')

        # CH07: offline senden -> reconnect -> DB
        netz(False); time.sleep(6)
        sende_text(d, f'{TEST_PREFIX} offline-ch07')
        time.sleep(3)
        ok_dlg = finde(d, 'OK', 3)
        if ok_dlg: ok_dlg.click()
        netz(True); time.sleep(20)
        cnt7 = db_count('offline-ch07')
        protokoll('CH07', f'Offline-Nachricht nach Reconnect am Server (count={cnt7})', cnt7 >= 1)

        # CH12: Netz-Flattern, Chat offen
        for _ in range(4):
            netz(False); time.sleep(3); netz(True); time.sleep(3)
        time.sleep(10)
        crash = adb('shell', 'logcat', '-d', '-b', 'crash')
        fatal = 'FATAL EXCEPTION' in crash and PAKET in crash
        protokoll('CH12', 'Netz-Flattern im Chat: App stabil, kein FATAL', app_laeuft() and not fatal)

    finally:
        try: d.quit()
        except Exception: pass
        netz(True)
        django(f"from webinterface.models import ChatMessage; ChatMessage.objects.using('property_1').filter(text__contains='{TEST_PREFIX}').delete()")

    sys.exit(_protokoll.abschluss())


if __name__ == '__main__':
    main()
