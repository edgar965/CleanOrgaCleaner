# -*- coding: utf-8 -*-
"""Sprachunabhängige Oberflächen-Begriffe für die Appium-Tests.

Die App übersetzt ihre Oberfläche (Localization/Sprachen/Texte*.cs). Welche
Sprache läuft, hängt am Konto und an einer gespeicherten Einstellung - nicht
am Test. Suchte ein Test fest nach der englischen Beschriftung, scheiterte er
auf einem deutsch eingestellten Gerät an der Sprache statt an einem echten
Fehler: Am 08.08.2026 meldete die ganze Foto-Suite "Login fehlgeschlagen",
obwohl nur der Knopf "Anmelden" statt "Login" hieß.

Deshalb schlägt ``common.finde`` jeden gesuchten Text hier nach und probiert
alle gleichbedeutenden Beschriftungen durch.
"""


class Begriffe:
    """Gruppen gleichbedeutender Beschriftungen (Deutsch/Englisch).

    Bewusst NICHT aufgenommen sind Wörter, deren Abwesenheit geprüft wird -
    etwa "Auftrag": Der Test AF03 stellt sicher, dass das alte Wort
    verschwunden ist, und darf "Neue Aufgabe" nicht als Treffer werten.
    """

    GRUPPEN = (
        ('Anmelden', 'Login'),
        ('Abmelden', 'Logout'),
        ('Heute', 'Today'),
        ('Neue Aufgabe', 'New Task'),
        ('Einstellungen', 'Settings'),
        ('Foto hinzufügen', 'Add photo'),
        ('Fotos', 'Photos'),
        ('Anmerkungen', 'Notes'),
        ('Abbrechen', 'Cancel'),
        ('Kamera', 'Camera'),
        ('Galerie', 'Gallery'),
        ('Ja', 'Yes'),
        ('Nein', 'No'),
        ('Datum', 'Date'),
        ('Sprache', 'Language'),
        ('Beenden', 'Finish'),
        ('Keine Aufgaben', 'No tasks'),
        ('Mitteilungen', 'Notifications'),
        ('Push-Mitteilungen', 'Push notifications'),
        ('Aktiviert', 'Enabled'),
        ('Nicht aktiviert', 'Not enabled'),
        ('Nicht aktiv', 'Not active'),
        ('Sprache auswählen', 'Select language'),
    )

    @classmethod
    def varianten(cls, text: str) -> tuple:
        """Alle Beschriftungen, die dasselbe bedeuten wie ``text``.

        Der gesuchte Text steht immer an erster Stelle, damit die bisherige
        Reihenfolge (und damit das Verhalten bei mehrdeutigen Bildschirmen)
        erhalten bleibt. Unbekannte Texte werden unverändert durchgereicht.
        """
        klein = text.strip().lower()
        for gruppe in cls.GRUPPEN:
            if any(klein == wort.lower() for wort in gruppe):
                rest = [w for w in gruppe if w.lower() != klein]
                return (text, *rest)
        return (text,)
