import {
  Component, Input, ViewChild, ElementRef, AfterViewInit, OnDestroy,
  inject, signal, ChangeDetectionStrategy,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subject, Subscription, debounceTime, distinctUntilChanged } from 'rxjs';
import { LeafletLoaderService } from '@shared/services/leaflet-loader.service';

type MapStatus = 'no-address' | 'loading' | 'ok' | 'not-found' | 'error';

const LEAFLET_IMG = 'https://unpkg.com/leaflet@1.9.4/dist/images';

@Component({
  selector: 'app-shop-map',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mapcard">
      <div #mapEl class="mapcard__map"></div>

      @switch (status()) {
        @case ('no-address') {
          <div class="state">📍 Renseignez l'adresse de la boutique pour afficher la carte.</div>
        }
        @case ('loading') {
          <div class="state">Chargement de la carte…</div>
        }
        @case ('not-found') {
          <div class="state">Adresse introuvable sur la carte. Vérifiez l'adresse, le code postal et la ville.</div>
        }
        @case ('error') {
          <div class="state">⚠️ Impossible de charger la carte (réseau indisponible).</div>
        }
        @case ('ok') {
          @if (label()) { <div class="found">📌 {{ label() }}</div> }
        }
      }
    </div>
  `,
  styles: [`
    .mapcard { display: flex; flex-direction: column; gap: var(--space-2); }
    .mapcard__map {
      width: 100%; height: 320px; border-radius: var(--radius-md);
      border: 1px solid var(--color-border-subtle); background: var(--color-surface-sunken);
      z-index: 0;
    }
    .state {
      font-size: var(--font-size-sm); color: var(--color-text-secondary);
      background: var(--color-surface-sunken); border: 1px dashed var(--color-border-default);
      border-radius: var(--radius-md); padding: var(--space-3); line-height: 1.5;
    }
    .found { font-size: var(--font-size-xs); color: var(--color-text-muted); }
  `],
})
export class ShopMapComponent implements AfterViewInit, OnDestroy {
  @ViewChild('mapEl') private mapEl!: ElementRef<HTMLDivElement>;

  private readonly loader = inject(LeafletLoaderService);

  readonly status = signal<MapStatus>('no-address');
  readonly label = signal<string>('');

  private readonly query$ = new Subject<string>();
  private sub?: Subscription;
  private map?: any;
  private marker?: any;
  private currentQuery = '';
  private viewReady = false;

  @Input() set query(value: string | null | undefined) {
    this.currentQuery = (value ?? '').trim();
    if (this.viewReady) this.query$.next(this.currentQuery);
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    // Nominatim : max ~1 req/s -> le debounce protège l'API.
    this.sub = this.query$
      .pipe(debounceTime(700), distinctUntilChanged())
      .subscribe(q => this.locate(q));
    this.locate(this.currentQuery);
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
    this.map?.remove?.();
  }

  private async locate(q: string): Promise<void> {
    if (!q) { this.status.set('no-address'); return; }
    this.status.set('loading');
    try {
      await this.loader.load();
      const L = (window as any).L;
      this.ensureMap(L);

      const url = `https://nominatim.openstreetmap.org/search?format=json&limit=1&q=${encodeURIComponent(q)}`;
      const res = await fetch(url, { headers: { 'Accept-Language': 'fr' } });
      const data = await res.json();

      if (Array.isArray(data) && data.length) {
        const hit = data[0];
        this.setMarker(L, parseFloat(hit.lat), parseFloat(hit.lon), hit.display_name ?? q);
        this.label.set(hit.display_name ?? '');
        this.status.set('ok');
      } else {
        this.status.set('not-found');
      }
    } catch {
      this.status.set('error');
    }
  }

  private ensureMap(L: any): void {
    if (this.map) return;
    this.map = L.map(this.mapEl.nativeElement, { scrollWheelZoom: false }).setView([46.6, 2.4], 5);
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '© OpenStreetMap',
    }).addTo(this.map);
    setTimeout(() => this.map.invalidateSize(), 0);
  }

  private setMarker(L: any, lat: number, lon: number, title: string): void {
    this.map.setView([lat, lon], 16);
    if (this.marker) this.marker.remove();
    const icon = L.icon({
      iconUrl: `${LEAFLET_IMG}/marker-icon.png`,
      iconRetinaUrl: `${LEAFLET_IMG}/marker-icon-2x.png`,
      shadowUrl: `${LEAFLET_IMG}/marker-shadow.png`,
      iconSize: [25, 41], iconAnchor: [12, 41], popupAnchor: [1, -34], shadowSize: [41, 41],
    });
    this.marker = L.marker([lat, lon], { icon }).addTo(this.map).bindPopup(title);
    setTimeout(() => this.map.invalidateSize(), 0);
  }
}
