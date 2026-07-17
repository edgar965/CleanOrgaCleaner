# -*- coding: utf-8 -*-
"""Loggt den Emulator als edgar ein und laesst die App laufen (Firestore-Listener
startet nach dem Login). Danach extern: logcat pruefen + Firestore-Injektion."""
import sys, time
from common import treiber, login, adb, finde, PAKET, ACTIVITY

d = treiber()
try:
    adb('shell', 'am', 'force-stop', PAKET)
    adb('shell', 'am', 'start', '-n', f'{PAKET}/{ACTIVITY}')
    time.sleep(6)
    if finde(d, 'Today', 4) is not None or finde(d, 'Start', 3) is not None:
        print('LOGIN bereits eingeloggt')
    else:
        ok = login(d, '1', 'edgar', 'edgar')
        print('LOGIN', ok)
    # Firestore-Init (Token holen -> Firebase-Auth -> Listener) laufen lassen
    time.sleep(8)
    print('READY (App laeuft, Firestore-Init sollte durch sein)')
finally:
    try:
        d.quit()
    except Exception:
        pass
