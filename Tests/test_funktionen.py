# -*- coding: utf-8 -*-
"""Funktions-Testsuite für CleanOrgaCleaner - deckt die Client-Funktionen ab,
die nicht spezifisch Offline/Chat sind (Navigation, Arbeitszeit, Aufgaben,
Auftrag, Einstellungen, Logout). Ergänzt test_offline.py und test_chat.py.

Aufruf: python test_funktionen.py   (Emulator + Appium + APK vorausgesetzt)
"""
import sys
import time
from common import (netz, app_neustart, treiber, login, navigiere, finde, finde_desc,
                    edittexts, app_laeuft, kein_fatal, screenshot, Protokoll, AppiumBy)

p = Protokoll()


def main():
    print('=== Funktions-Testsuite CleanOrgaCleaner ===', flush=True)
    netz(True)
    d = treiber()
    try:
        app_neustart()
        p('F01', 'Login online (tom/tom)', login(d))
        screenshot(d, 'f01_login')

        # F02: Aufgabenliste "Heute" wird angezeigt
        heute_da = finde(d, 'Today', 8) is not None
        p('F02', 'Heute-Seite geladen', heute_da)

        # F03: Menü-Navigation zu allen Hauptbereichen
        chat_ok = navigiere(d, 'Chat') and finde(d, 'Admin', 6) is not None
        p('F03', 'Navigation zu Chat', chat_ok)
        screenshot(d, 'f03_chat')

        auftrag_ok = navigiere(d, 'Task')
        p('F04', 'Navigation zu Auftrag/Task', auftrag_ok)
        screenshot(d, 'f04_auftrag')

        settings_ok = navigiere(d, 'Settings')
        p('F05', 'Navigation zu Einstellungen', settings_ok)
        screenshot(d, 'f05_settings')

        # F06: Version wird angezeigt (echte Build-Version, nicht mehr "1.52")
        # Version steht am Seitenende -> erst nach unten scrollen
        d.swipe(400, 1500, 400, 400, 500); time.sleep(1)
        d.swipe(400, 1500, 400, 400, 500); time.sleep(1)
        version_da = finde(d, '1.6', 5) is not None or finde(d, 'Version', 5) is not None
        p('F06', 'Version in Einstellungen sichtbar (nicht hartcodiert 1.52)',
          version_da and finde(d, '1.52', 2) is None)

        # F07: Biometrie-Sektion / Sprach-Auswahl vorhanden
        # (Elemente sind gerätespezifisch; wir prüfen die Seite grob)
        einstellungen_inhalt = (finde(d, 'Language', 4) is not None
                                or finde(d, 'Sprache', 2) is not None
                                or finde_desc(d, 'Language', 3) is not None)
        p('F07', 'Sprach-Auswahl in Einstellungen', einstellungen_inhalt)

        # F08: zurück zu Heute + Arbeitszeit starten
        navigiere(d, 'Today')
        # laufende Arbeit ggf. beenden
        fin = finde(d, 'Finish', 3)
        if fin:
            fin.click(); time.sleep(2)
            ja = finde(d, 'Yes', 4)
            if ja: ja.click(); time.sleep(3)
        start = finde(d, 'Start', 6)
        if start:
            start.click(); time.sleep(3)
            gestartet = finde(d, 'Finish', 6) is not None
            p('F08', 'Arbeitszeit starten (Button wechselt auf Finish)', gestartet)
        else:
            p('F08', 'Arbeitszeit starten', False, 'Start-Button nicht gefunden')
        screenshot(d, 'f08_arbeit')

        # F09: Arbeitszeit beenden
        fin = finde(d, 'Finish', 5)
        if fin:
            fin.click(); time.sleep(2)
            ja = finde(d, 'Yes', 4)
            if ja: ja.click(); time.sleep(3)
            beendet = finde(d, 'Start', 6) is not None
            p('F09', 'Arbeitszeit beenden (Button wechselt auf Start)', beendet)
        else:
            p('F09', 'Arbeitszeit beenden', False, 'Finish-Button nicht gefunden')

        # F10: App stabil über den gesamten Durchlauf (kein FATAL im Crash-Log)
        p('F10', 'Keine FATAL EXCEPTION während des Durchlaufs', app_laeuft() and kein_fatal())

        # F11: Logout
        logout = None
        if navigiere(d, 'Logout'):
            logout = True
            # Bestätigungsdialog quittieren
            ja = finde(d, 'Yes', 5)
            if ja: ja.click()
        else:
            navigiere(d, 'Settings')
            lo = finde(d, 'Logout', 5)
            if lo:
                lo.click(); time.sleep(2)
                ja = finde(d, 'Yes', 4)
                if ja: ja.click()
                logout = True
        time.sleep(5)
        zur_login = finde(d, 'Login', 8) is not None
        p('F11', 'Logout führt zur Login-Seite', bool(logout) and zur_login)
        screenshot(d, 'f11_logout')

    finally:
        try:
            d.quit()
        except Exception:
            pass
        netz(True)

    sys.exit(p.abschluss())


if __name__ == '__main__':
    main()
