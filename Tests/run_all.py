# -*- coding: utf-8 -*-
"""Führt alle Appium-Testsuiten nacheinander aus (ein Emulator, sequenziell,
da alle dieselbe Appium-Session/den selben Emulator nutzen).

Aufruf:  python run_all.py           (alle Suiten)
         python run_all.py chat      (nur bestimmte Suiten)

Voraussetzungen siehe README.md.
"""
import subprocess
import sys

SUITEN = {
    'offline': 'test_offline.py',
    'chat': 'test_chat.py',
    'funktionen': 'test_funktionen.py',
}


def main():
    auswahl = sys.argv[1:] or list(SUITEN.keys())
    ergebnis = {}
    for name in auswahl:
        skript = SUITEN.get(name)
        if not skript:
            print(f'Unbekannte Suite: {name} (verfügbar: {", ".join(SUITEN)})')
            continue
        print(f'\n{"="*60}\n### Suite: {name} ({skript})\n{"="*60}', flush=True)
        code = subprocess.run([sys.executable, skript]).returncode
        ergebnis[name] = code

    print(f'\n{"="*60}\n### GESAMT\n{"="*60}', flush=True)
    for name, code in ergebnis.items():
        print(f'{"OK  " if code == 0 else "FAIL"}  {name}', flush=True)
    sys.exit(0 if all(c == 0 for c in ergebnis.values()) else 1)


if __name__ == '__main__':
    main()
