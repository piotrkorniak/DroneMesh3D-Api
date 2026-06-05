import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  inject,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import {
  debounceTime,
  distinctUntilChanged,
  filter,
  switchMap,
  tap,
  catchError,
  of,
  Subject,
} from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

export interface NominatimResult {
  place_id: number;
  display_name: string;
  name: string;
  lat: string;
  lon: string;
}

export interface LocationSelectedEvent {
  lat: number;
  lon: number;
  name: string;
}

@Component({
  selector: 'app-map-search',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './map-search.component.html',
  styleUrl: './map-search.component.scss',
})
export class MapSearchComponent {
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);

  readonly locationSelected = output<LocationSelectedEvent>();

  readonly results = signal<NominatimResult[]>([]);
  readonly isLoading = signal(false);
  readonly showDropdown = signal(false);

  private readonly searchSubject = new Subject<string>();
  readonly inputRef = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  constructor() {
    this.searchSubject
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        debounceTime(300),
        distinctUntilChanged(),
        filter(query => query.length >= 3),
        tap(() => this.isLoading.set(true)),
        switchMap(query =>
          this.http
            .get<NominatimResult[]>(
              `https://nominatim.openstreetmap.org/search?format=json&q=${encodeURIComponent(query)}&limit=5`,
              { headers: { 'Accept-Language': 'pl' } }
            )
            .pipe(
              catchError(() => {
                this.isLoading.set(false);
                return of([]);
              })
            )
        )
      )
      .subscribe(results => {
        this.results.set(results);
        this.isLoading.set(false);
        this.showDropdown.set(results.length > 0);
      });
  }

  onInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value.trim();
    if (value.length < 3) {
      this.results.set([]);
      this.showDropdown.set(false);
      this.isLoading.set(false);
      return;
    }
    this.searchSubject.next(value);
  }

  onKeydown(event: KeyboardEvent): void {
    if (event.key === 'Enter') {
      event.preventDefault();
      const value = (event.target as HTMLInputElement).value.trim();
      if (value.length >= 3) {
        this.isLoading.set(true);
        this.searchSubject.next(value);
      }
    }
    if (event.key === 'Escape') {
      this.showDropdown.set(false);
    }
  }

  selectResult(result: NominatimResult): void {
    const lat = parseFloat(result.lat);
    const lon = parseFloat(result.lon);
    const name = result.name || result.display_name.split(',')[0];

    this.locationSelected.emit({ lat, lon, name });
    this.showDropdown.set(false);
    this.results.set([]);

    // Update the input value
    const inputEl = this.inputRef();
    if (inputEl) {
      inputEl.nativeElement.value = name;
    }
  }

  onBlur(): void {
    // Delay to allow click on result to fire first
    setTimeout(() => this.showDropdown.set(false), 200);
  }

  triggerSearch(): void {
    const inputEl = this.inputRef();
    if (!inputEl) return;
    const value = inputEl.nativeElement.value.trim();
    if (value.length >= 3) {
      this.isLoading.set(true);
      this.searchSubject.next(value);
    }
  }

  truncateDisplayName(displayName: string): string {
    return displayName.length > 60
      ? displayName.substring(0, 60) + '…'
      : displayName;
  }
}
