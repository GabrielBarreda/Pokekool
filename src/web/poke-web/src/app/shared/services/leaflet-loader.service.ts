import { Injectable } from '@angular/core';

/**
 * Charge Leaflet (CSS + JS) depuis le CDN une seule fois. 100% gratuit, sans clé.
 * Résout quand la globale `L` est disponible.
 */
@Injectable({ providedIn: 'root' })
export class LeafletLoaderService {
  private static readonly VERSION = '1.9.4';
  private promise?: Promise<void>;

  load(): Promise<void> {
    if (this.promise) return this.promise;

    this.promise = new Promise<void>((resolve, reject) => {
      const w = window as unknown as { L?: unknown };
      if (w.L) { resolve(); return; }

      const base = `https://unpkg.com/leaflet@${LeafletLoaderService.VERSION}/dist`;

      if (!document.getElementById('leaflet-css')) {
        const link = document.createElement('link');
        link.id = 'leaflet-css';
        link.rel = 'stylesheet';
        link.href = `${base}/leaflet.css`;
        document.head.appendChild(link);
      }

      const script = document.createElement('script');
      script.src = `${base}/leaflet.js`;
      script.async = true;
      script.onload = () => resolve();
      script.onerror = () => { this.promise = undefined; reject(new Error('leaflet-load-failed')); };
      document.head.appendChild(script);
    });

    return this.promise;
  }
}
