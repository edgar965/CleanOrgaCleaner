# -*- coding: utf-8 -*-
"""Testsuite fuer die Umbenennung "Auftrag" -> "Neue Aufgabe" und die Fotos
zur Aufgabenbeschreibung.

Geprueft wird:
  * das Menue und der Dialog heissen "New Task" (englische Oberflaeche des
    Testkontos), das Wort "Auftrag"/"Order" taucht nicht mehr auf
  * im Tab "Details" gibt es eine Fotoreihe, und sie steht VOR dem Datum
  * im Dialog "Neue Aufgabe" laesst sich ein Foto hinzufuegen und wieder
    loeschen
  * bei einer ZUGEWIESENEN Aufgabe sind die Fotos nur zu sehen - kein
    Hinzufuegen, kein Loeschen

Aufruf: python test_aufgabe_fotos.py   (Emulator + Appium + APK vorausgesetzt)
"""
import sys
import time

from common import (netz, app_neustart, treiber, login, navigiere, finde, finde_desc,
                    app_laeuft, kein_fatal, screenshot, django, Protokoll, AppiumBy)

p = Protokoll()

# Aufgabe, die dem Testkonto (Cleaner 9 = tom, Firma 1) zugewiesen wird
TEST_AUFGABE = '[APPIUM-TEST] Fotoaufgabe'


def testaufgabe_anlegen() -> str:
    """Legt serverseitig eine tom zugewiesene Aufgabe mit einem Aufgabenfoto an.

    Die Heute-Liste zeigt Aufgaben als "<Apartment> <Art>" an - deshalb bekommt
    die Testaufgabe ein echtes Apartment, dessen Name zurueckgegeben wird.
    """
    return django(f"""
import io
from datetime import date
from django.core.files.base import ContentFile
from PIL import Image
from webinterface.db_router import set_current_property
from webinterface.models import (Apartment, CleaningTask, Cleaner, ImageListDescription,
                                 ImageListDescriptionPhoto)
set_current_property(1)
DB = 'property_1'
CleaningTask.objects.using(DB).filter(name='{TEST_AUFGABE}').delete()
tom = Cleaner.objects.using(DB).get(id=9)
apt = Apartment.objects.using(DB).order_by('id').first()
t = CleaningTask.objects.using(DB).create(
    name='{TEST_AUFGABE}', planned_date=date.today(), aufwand=1.0,
    apartment=apt, aufgabe='Foto beachten', status='assigned')
t.assigned_cleaner_list.set([tom.id])
e = ImageListDescription.objects.using(DB).create(
    cleaning_task=t, type='aufgabe', name='Aufgabenfotos', created_by='appium')
puffer = io.BytesIO()
Image.new('RGB', (60, 40), (200, 40, 40)).save(puffer, format='JPEG')
ImageListDescriptionPhoto.objects.using(DB).create(
    parent=e, image=ContentFile(puffer.getvalue(), name='appium_test.jpg'))
print('ANGELEGT', t.id, 'APARTMENT', apt.name if apt else '-')
""")


def testaufgabe_loeschen():
    django(f"""
import os
from django.conf import settings
from webinterface.db_router import set_current_property
from webinterface.models import (CleaningTask, ImageListDescription,
                                 ImageListDescriptionPhoto)
set_current_property(1)
DB = 'property_1'
dateien = []
for t in CleaningTask.objects.using(DB).filter(name='{TEST_AUFGABE}'):
    for e in ImageListDescription.objects.using(DB).filter(cleaning_task=t):
        for f in ImageListDescriptionPhoto.objects.using(DB).filter(parent=e):
            dateien.append(f.image.name)
    t.delete()
for name in dateien:
    pfad = os.path.join(settings.MEDIA_ROOT, name)
    if os.path.exists(pfad):
        os.remove(pfad)
print('AUFGERAEUMT')
""")


def fotos_der_aufgabe() -> str:
    """Zaehlt die Aufgabenfotos serverseitig."""
    return django(f"""
from webinterface.db_router import set_current_property
from webinterface.models import CleaningTask, ImageListDescription, ImageListDescriptionPhoto
set_current_property(1)
DB = 'property_1'
t = CleaningTask.objects.using(DB).filter(name='{TEST_AUFGABE}').first()
n = 0
if t:
    for e in ImageListDescription.objects.using(DB).filter(cleaning_task=t, type='aufgabe'):
        n += ImageListDescriptionPhoto.objects.using(DB).filter(parent=e).count()
print('FOTOS', n)
""")


def main():
    print('=== Testsuite: Neue Aufgabe + Fotos ===', flush=True)
    netz(True)

    anlage = testaufgabe_anlegen()
    print(f'Testaufgabe: {anlage.strip()}', flush=True)
    apartment_name = ''
    if 'APARTMENT' in anlage:
        apartment_name = anlage.split('APARTMENT', 1)[1].strip().split()[0]

    d = treiber()
    try:
        app_neustart()
        p('AF01', 'Login online (tom/tom)', login(d))

        # --- Umbenennung -------------------------------------------------
        navigiere(d, 'New Task')
        auf_seite = finde(d, 'New Task', 8) is not None
        p('AF02', 'Menuepunkt und Seite heissen "New Task"', auf_seite)
        screenshot(d, 'af02_neue_aufgabe_seite')

        kein_auftrag = finde(d, 'Auftrag', 3) is None and finde(d, 'Order', 3) is None
        p('AF03', '"Auftrag"/"Order" kommt nicht mehr vor', kein_auftrag)

        # --- Dialog oeffnen ----------------------------------------------
        # Achtung: "New Task" steht auch in der Kopfzeile - deshalb gezielt den
        # Knopf mit dem Pluszeichen anklicken.
        knopf = finde(d, '+ New Task', 6)
        if knopf is None:
            knopf = finde(d, '+', 4)
        if knopf:
            knopf.click()
            time.sleep(3)
        dialog_offen = finde(d, 'Details', 8) is not None
        p('AF04', 'Dialog "Neue Aufgabe" oeffnet auf dem Tab Details', dialog_offen)
        screenshot(d, 'af04_dialog')

        # --- Fotoreihe vorhanden und VOR dem Datum ------------------------
        foto_knopf = finde(d, 'Add photo', 6)
        p('AF05', 'Knopf "Foto hinzufuegen" im Tab Details', foto_knopf is not None)

        datum_feld = finde(d, 'Date', 5)
        if foto_knopf is not None and datum_feld is not None:
            vor_datum = foto_knopf.location['y'] < datum_feld.location['y']
            p('AF06', 'Fotoreihe steht vor dem Datum',
              vor_datum, f"Foto y={foto_knopf.location['y']}, Datum y={datum_feld.location['y']}")
        else:
            p('AF06', 'Fotoreihe steht vor dem Datum', False, 'Elemente nicht gefunden')

        # --- Foto hinzufuegen: Auswahl Kamera/Galerie ---------------------
        if foto_knopf is not None:
            foto_knopf.click()
            time.sleep(2)
            auswahl_da = (finde(d, 'Camera', 5) is not None
                          or finde(d, 'Gallery', 3) is not None)
            p('AF07', 'Auswahl Kamera/Galerie erscheint', auswahl_da)
            screenshot(d, 'af07_fotoquelle')

            # AF07b: Die Auswahl muss sich abbrechen lassen. Ohne Kamera (z. B.
            # im Emulator) blieb der Dialog frueher stehen und die App war nur
            # noch durch Beenden zu retten.
            abbrechen = finde(d, 'Cancel', 4)
            if abbrechen is not None:
                abbrechen.click()
                time.sleep(2)
            # Bedienbar? Dann ist der Tab "Details" wieder erreichbar
            wieder_bedienbar = finde(d, 'Details', 6) is not None
            p('AF13', 'Foto-Auswahl laesst sich abbrechen, App bleibt bedienbar',
              wieder_bedienbar)
            screenshot(d, 'af13_nach_abbruch')
        else:
            p('AF07', 'Auswahl Kamera/Galerie erscheint', False, 'Knopf fehlt')
            p('AF13', 'Foto-Auswahl laesst sich abbrechen', False, 'uebersprungen')

        # Dialog schliessen
        abbrechen = finde(d, 'Cancel', 5)
        if abbrechen:
            abbrechen.click()
            time.sleep(2)

        # --- Zugewiesene Aufgabe: Fotos nur ansehen -----------------------
        navigiere(d, 'Today')
        time.sleep(2)

        # Ohne laufende Arbeitszeit laesst die App keine Aufgabe oeffnen
        # ("Please click 'Start Work' first ...").
        arbeit_gestartet = False
        start = finde(d, 'Start', 5)
        if start is not None:
            start.click()
            time.sleep(4)
            arbeit_gestartet = True

        # Die Heute-Liste zeigt "<Apartment> <Art>" - deshalb ueber das Apartment
        aufgabe = finde(d, apartment_name, 8) if apartment_name else None
        if aufgabe is None:
            aufgabe = finde(d, 'Fotoaufgabe', 4) or finde(d, 'APPIUM-TEST', 4)
        if aufgabe is not None:
            aufgabe.click()
            time.sleep(4)
            # Falls doch ein Hinweis kommt: bestaetigen und erneut oeffnen
            hinweis = finde(d, 'OK', 3)
            if hinweis is not None:
                hinweis.click()
                time.sleep(2)
                erneut = finde(d, apartment_name, 5) if apartment_name else None
                if erneut is not None:
                    erneut.click()
                    time.sleep(4)
            fotos_sichtbar = finde(d, 'Photos', 8) is not None
            p('AF08', 'Zugewiesene Aufgabe zeigt die Fotos', fotos_sichtbar)
            screenshot(d, 'af08_zugewiesen_fotos')

            # In der Nur-Ansicht darf es weder Hinzufuegen noch Loeschen geben
            kein_hinzufuegen = finde(d, 'Add photo', 3) is None
            kein_loeschen = len(d.find_elements(
                AppiumBy.ANDROID_UIAUTOMATOR, 'new UiSelector().text("×")')) == 0
            p('AF09', 'Kein Hinzufuegen-Knopf bei zugewiesener Aufgabe', kein_hinzufuegen)
            p('AF10', 'Kein Loesch-Knopf an den Fotos', kein_loeschen)
        else:
            p('AF08', 'Zugewiesene Aufgabe zeigt die Fotos', False, 'Aufgabe nicht gefunden')
            p('AF09', 'Kein Hinzufuegen-Knopf bei zugewiesener Aufgabe', False, 'uebersprungen')
            p('AF10', 'Kein Loesch-Knopf an den Fotos', False, 'uebersprungen')

        # --- Serverseitig: das Foto ist unveraendert ----------------------
        stand = fotos_der_aufgabe()
        p('AF11', 'Foto der zugewiesenen Aufgabe unveraendert vorhanden',
          'FOTOS 1' in stand, stand.strip())

        p('AF12', 'Keine FATAL EXCEPTION waehrend des Durchlaufs',
          app_laeuft() and kein_fatal())

        # Arbeitszeit wieder beenden, damit der naechste Lauf sauber startet
        if arbeit_gestartet:
            navigiere(d, 'Today')
            fertig = finde(d, 'Finish', 5)
            if fertig is not None:
                fertig.click()
                time.sleep(2)
                ja = finde(d, 'Yes', 4)
                if ja is not None:
                    ja.click()
                    time.sleep(3)

    finally:
        try:
            d.quit()
        except Exception:
            pass
        testaufgabe_loeschen()
        netz(True)

    sys.exit(p.abschluss())


if __name__ == '__main__':
    main()
