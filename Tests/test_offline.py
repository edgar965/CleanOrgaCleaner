# -*- coding: utf-8 -*-
"""Automatisierte Offline-Tests für CleanOrgaCleaner (Android-Emulator + Appium).

Deckt die automatisierbaren Fälle aus offline_testcases.md ab (TC01-TC09, TC11).
Testzugang: Property 1 / tom / tom (offizieller Test-User, Sprache en).
Netzwerk-Simulation: adb svc wifi/data. Server-Verifikation per SSH (Django-ORM).

Aufruf: python appium_offline_tests.py
"""
import sys
import time

from common import (ACTIVITY, PAKET, TEST_PREFIX, AppiumBy, adb, app_laeuft,
                    django, finde, login, netz, oeffne_chat_mit,
                    screenshot, sende_text, treiber, warte_auf_server,
                    Protokoll)

_protokoll = Protokoll()

# Der übrige Code sucht historisch mit finde_text; common.finde kann dasselbe
# und kennt zusätzlich die deutschen Beschriftungen.
finde_text = finde


def warte_auf_start(timeout=25) -> bool:
    ende = time.time() + timeout
    while time.time() < ende:
        if app_laeuft():
            return True
        time.sleep(1)
    return False


def app_stop():
    adb('shell', 'am', 'force-stop', PAKET)
    time.sleep(1)


def app_start():
    adb('shell', 'am', 'start', '-n', f'{PAKET}/{ACTIVITY}')
    warte_auf_start()
    time.sleep(3)


def finde_klasse(d, klasse, index, timeout=15):
    ende = time.time() + timeout
    while time.time() < ende:
        els = d.find_elements(AppiumBy.CLASS_NAME, klasse)
        if len(els) > index:
            return els[index]
        time.sleep(1)
    return None


def protokoll(nr, titel, ok, detail=''):
    """Ergebniszeile - die Sammlung liegt in der Protokoll-Klasse (common)."""
    _protokoll(nr, titel, ok, detail)


def main():
    print('=== Offline-Testsuite CleanOrgaCleaner ===', flush=True)
    netz(True)
    app_stop()
    adb('shell', 'pm', 'clear', PAKET)  # frischer Zustand für TC02
    time.sleep(1)

    d = treiber()
    try:
        # ---- TC02: Offline-Kaltstart ohne Cache ----
        netz(False)
        app_stop(); app_start()
        felder = finde_klasse(d, 'android.widget.EditText', 2, timeout=20)
        lebt = app_laeuft()
        protokoll('TC02', 'Offline-Kaltstart ohne Cache: kein Crash, Login-Seite da',
                  lebt and felder is not None)
        # Login offline versuchen -> saubere Fehlermeldung, kein Crash
        if felder is not None:
            eintraege = d.find_elements(AppiumBy.CLASS_NAME, 'android.widget.EditText')
            eintraege[0].send_keys('1'); eintraege[1].send_keys('tom'); eintraege[2].send_keys('tom')
            knopf = finde_text(d, 'Login', 5)
            if knopf: knopf.click()
            time.sleep(10)
            lebt = app_laeuft()
            protokoll('TC02b', 'Offline-Login ohne Cache: App stabil', lebt)
            screenshot(d, 'tc02')

        # ---- Grundlage: Online-Login + Aufgaben laden (Cache füllen) ----
        netz(True)
        time.sleep(4)
        app_stop(); app_start()
        ok = login(d)
        protokoll('T-LOGIN', 'Online-Login tom/tom', ok)
        screenshot(d, 'login')
        time.sleep(5)  # today-data + Cache

        # ---- TC05/TC06-Vorbereitung: Arbeit starten (online zuruecksetzen) ----
        # Falls Arbeit schon laeuft: erst beenden (Finish + Yes)
        stopknopf = finde_text(d, 'Finish', timeout=3)
        if stopknopf:
            stopknopf.click(); time.sleep(2)
            ja = finde_text(d, 'Yes', 5)
            if ja: ja.click(); time.sleep(3)

        # ---- TC03: Online -> Offline, Inhalte bleiben ----
        netz(False)
        time.sleep(8)
        d.swipe(400, 600, 400, 1400, 600)  # Pull-to-Refresh
        time.sleep(8)
        lebt = app_laeuft()
        # Inhalt noch da? Entweder Aufgaben-Buttons oder "No tasks" - aber NICHT leer/Fehlerseite
        inhalt = finde_text(d, 'Start', 5) or finde_text(d, 'No tasks', 5) or finde_text(d, 'Today', 5)
        protokoll('TC03', 'Offline-Refresh: App stabil, Inhalt bleibt', lebt and inhalt is not None)
        screenshot(d, 'tc03')

        # ---- TC09a: Offline-Banner sichtbar ----
        banner = finde_text(d, 'Offline', timeout=20)
        protokoll('TC09a', 'Offline-Banner erscheint nach Netztrennung', banner is not None)

        # ---- TC04 + TC01: App-Neustart offline -> Cache-Login + Aufgaben ----
        app_stop(); app_start()
        time.sleep(8)
        lebt = app_laeuft()
        inhalt = finde_text(d, 'Start', 10) or finde_text(d, 'No tasks', 5) or finde_text(d, 'Finish', 5)
        protokoll('TC01/TC04', 'Offline-Kaltstart mit Cache: Offline-Login + Inhalt da (Cache nicht zerstoert)',
                  lebt and inhalt is not None)
        screenshot(d, 'tc04')

        # ---- TC05: Arbeitsbeginn offline ----
        startknopf = finde_text(d, 'Start', timeout=10)
        if startknopf:
            startknopf.click()
            time.sleep(4)
            hinweis = finde_text(d, 'Saved', 5) or finde_text(d, 'sync', 5) or finde_text(d, 'Finish', 5)
            lebt = app_laeuft()
            protokoll('TC05', 'Arbeitsbeginn offline: Hinweis/Queue, kein Crash', lebt and hinweis is not None)
            ok_knopf = finde_text(d, 'OK', 3)
            if ok_knopf:
                ok_knopf.click(); time.sleep(1)
        else:
            protokoll('TC05', 'Arbeitsbeginn offline', False, 'Start-Button nicht gefunden')
        screenshot(d, 'tc05')

        # ---- TC07: Chat offline an Admin ----
        # Verlauf und Senden über die gemeinsamen Fassungen: Die frühere
        # Eigenbau-Variante suchte den Senden-Knopf über den Text "Send" und
        # fiel sonst auf "den letzten Knopf" zurück. Bei offener Tastatur traf
        # das deren Enter-Taste, die in der App nichts auslöst - die Nachricht
        # blieb im Feld stehen und TC07b meldete dauerhaft Count=0.
        if oeffne_chat_mit(d, 'Admin'):
            abgeschickt = sende_text(d, f'{TEST_PREFIX} offline an admin')
            protokoll('TC07a', 'Chat-Nachricht offline abgesetzt: App stabil',
                      app_laeuft() and abgeschickt)
            screenshot(d, 'tc07')
        else:
            protokoll('TC07a', 'Chat offline', False, 'Admin-Chat nicht gefunden')

        # ---- TC06 + TC07b: Reconnect -> Queue synct (Server-Verifikation) ----
        netz(True)
        time.sleep(10)
        lebt = app_laeuft()
        protokoll('TC06a', 'Reconnect: App stabil', lebt)

        def am_server():
            antwort = django(
                "from webinterface.models import ChatMessage; "
                "print(ChatMessage.objects.using('property_1')"
                ".filter(text__contains='[APPIUM-TEST]').count())"
            ).strip()
            return antwort.isdigit() and int(antwort) >= 1

        # Aktiv warten: Die Warteschlange braucht nach dem Wiederverbinden
        # unterschiedlich lange (gemessen 15-30 s). Ein festes sleep(25)
        # meldete sporadisch "kam nicht an".
        angekommen = warte_auf_server(am_server, timeout=90)
        protokoll('TC07b', 'Offline-Chatnachricht am Server angekommen', angekommen)

        # ---- TC09b: Banner verschwindet nach Reconnect ----
        heute_tab = finde_text(d, 'Today', 10)
        if heute_tab:
            heute_tab.click(); time.sleep(5)
        banner_weg = finde_text(d, 'Offline', timeout=5) is None
        protokoll('TC09b', 'Offline-Banner verschwindet nach Reconnect', banner_weg)
        screenshot(d, 'tc09')

        # ---- TC11: Netz-Flattern (Connect-Race) ----
        for _ in range(5):
            netz(False); time.sleep(3)
            netz(True); time.sleep(3)
        time.sleep(20)
        lebt = app_laeuft()
        absturz = adb('shell', 'logcat', '-d', '-s', 'AndroidRuntime:E')
        fatal = 'FATAL EXCEPTION' in absturz and PAKET in absturz
        protokoll('TC11', 'Netz-Flattern 5x: App stabil, kein FATAL', lebt and not fatal)
        screenshot(d, 'tc11')

        # ---- Aufraeumen: Arbeit beenden (falls offen), Testnachrichten loeschen ----
        stopknopf = finde_text(d, 'Finish', 5)
        if stopknopf:
            stopknopf.click(); time.sleep(2)
            ja = finde_text(d, 'Yes', 5)
            if ja: ja.click(); time.sleep(3)
        geloescht = django(
            "from webinterface.models import ChatMessage; "
            "print(ChatMessage.objects.using('property_1').filter(text__contains='[APPIUM-TEST]').delete())"
        )
        print(f'Aufgeraeumt: {geloescht}', flush=True)

    finally:
        try:
            d.quit()
        except Exception:
            pass
        netz(True)

    sys.exit(_protokoll.abschluss())


if __name__ == '__main__':
    main()
