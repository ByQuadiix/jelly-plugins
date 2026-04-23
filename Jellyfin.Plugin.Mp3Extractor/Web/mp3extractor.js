/**
 * Mp3Extractor – Kontextmenü-Integration
 *
 * Dieses Skript klinkt sich in das Jellyfin-Web-UI ein und fügt dem
 * Item-Kontextmenü (3-Punkte-Menü) von Videos einen Button
 * „Als MP3 herunterladen" hinzu.
 *
 * Das Skript wird von Jellyfin automatisch geladen, weil es als
 * PluginPageInfo ohne DisplayName registriert ist.
 */
(function () {
    'use strict';

    /* ── Konstanten ─────────────────────────────────────────────────────── */
    const BUTTON_ID = 'mp3ExtractorDownloadBtn';
    const API_BASE  = '/Mp3Extractor';

    /* ── Hilfsfunktionen ─────────────────────────────────────────────────── */

    /**
     * Gibt den Download-URL für das angegebene Item zurück.
     * @param {string} itemId  Jellyfin Item-GUID
     * @returns {string}
     */
    function getDownloadUrl(itemId) {
        const apiKey = window.ApiClient ? window.ApiClient.accessToken() : '';
        const sep    = apiKey ? '?api_key=' + encodeURIComponent(apiKey) : '';
        return API_BASE + '/' + itemId + '/download' + sep;
    }

    /**
     * Prüft ob der übergebene MIME-Type oder Jellyfin-MediaType ein Video ist.
     * @param {object} item  Jellyfin BaseItem-DTO
     * @returns {boolean}
     */
    function isVideoItem(item) {
        if (!item) return false;
        const t = (item.MediaType || '').toLowerCase();
        const type = (item.Type || '').toLowerCase();
        return t === 'video' || type === 'movie' || type === 'episode' || type === 'video';
    }

    /**
     * Fügt den MP3-Download-Button in ein geöffnetes Kontextmenü ein,
     * sofern das Item ein Video ist.
     *
     * @param {HTMLElement} menu  Das DOM-Element des Kontextmenüs
     * @param {object}      item  Das Jellyfin-Item-Objekt
     */
    function injectButton(menu, item) {
        if (!isVideoItem(item)) return;
        if (menu.querySelector('#' + BUTTON_ID)) return;   // bereits injiziert

        /* Versuche, den Download-Button als Referenzpunkt zu finden */
        const refButton = menu.querySelector('[data-id="download"]')
                       || menu.querySelector('button[href*="download"]')
                       || menu.lastElementChild;

        const btn = document.createElement('button');
        btn.id    = BUTTON_ID;
        btn.type  = 'button';
        btn.className = refButton ? refButton.className : 'listItem listItem-button';
        btn.style.cssText = 'display:flex;align-items:center;gap:0.5em;';

        /* Icon (SVG Musik-Noten) */
        const icon = document.createElement('span');
        icon.className = 'listItemIcon listItemIcon-transparent material-icons';
        icon.setAttribute('aria-hidden', 'true');
        icon.textContent = 'audio_file';   // Material-Icon, bereits in Jellyfin geladen

        const label = document.createElement('span');
        label.className = 'listItemBodyText';
        label.textContent = 'Als MP3 herunterladen';

        btn.appendChild(icon);
        btn.appendChild(label);

        btn.addEventListener('click', function () {
            const url = getDownloadUrl(item.Id);
            const link = document.createElement('a');
            link.href     = url;
            link.download = (item.Name || 'audio') + '.mp3';
            link.style.display = 'none';
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);

            /* Menü schließen */
            if (menu.close) {
                menu.close();
            } else {
                menu.dispatchEvent(new Event('close'));
            }
        });

        /* Button hinter dem Download-Button einfügen (oder am Ende) */
        if (refButton && refButton.parentNode === menu) {
            refButton.insertAdjacentElement('afterend', btn);
        } else {
            menu.appendChild(btn);
        }
    }

    /* ── Haupt-Hook: MutationObserver ────────────────────────────────────── */

    /**
     * Jellyfin öffnet Kontextmenüs als dynamisch eingefügte DOM-Elemente.
     * Ein MutationObserver auf `document.body` erkennt neue Menüs zuverlässig.
     */
    function startObserver() {
        const observer = new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(function (node) {
                    if (node.nodeType !== Node.ELEMENT_NODE) return;

                    /* Suche nach einem Kontextmenü-Container */
                    const menu = node.matches('.actionSheetContent, .contextMenu, .sheet-content')
                        ? node
                        : node.querySelector('.actionSheetContent, .contextMenu, .sheet-content');

                    if (!menu) return;

                    /* Hole das aktuelle Item aus dem Jellyfin-State */
                    resolveCurrentItem(function (item) {
                        injectButton(menu, item);
                    });
                });
            });
        });

        observer.observe(document.body, { childList: true, subtree: true });
    }

    /**
     * Versucht das aktuell fokussierte Item über verschiedene Jellyfin-
     * Interna zu ermitteln.
     *
     * @param {function} callback  Wird mit dem Item-Objekt (oder null) aufgerufen
     */
    function resolveCurrentItem(callback) {
        /* Möglichkeit 1: Das letzte per requirejs geladene View hat ein `currentItem` */
        try {
            const currentItemId = document
                .querySelector('[data-itemid]')
                ?.getAttribute('data-itemid')
                || document
                    .querySelector('[data-id]')
                    ?.getAttribute('data-id');

            if (currentItemId && window.ApiClient) {
                window.ApiClient.getItem(window.ApiClient.getCurrentUserId(), currentItemId)
                    .then(callback)
                    .catch(function () { callback(null); });
                return;
            }
        } catch (e) {
            // ignore
        }

        callback(null);
    }

    /* ── Start ────────────────────────────────────────────────────────────── */

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', startObserver);
    } else {
        startObserver();
    }

    console.info('[Mp3Extractor] Kontextmenü-Integration geladen.');
}());
