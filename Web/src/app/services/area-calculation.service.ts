import { Injectable } from '@angular/core';

/**
 * Calculates polygon area in hectares from GeoJSON coordinates
 * using the spherical excess formula for polygon area on Earth's surface.
 */
@Injectable({ providedIn: 'root' })
export class AreaCalculationService {
  /** Earth's radius in meters (WGS-84 mean radius) */
  private static readonly EARTH_RADIUS_M = 6371008.8;

  /**
   * Calculates the area of a polygon in hectares from GeoJSON coordinates.
   *
   * @param coordinates - Array of [longitude, latitude] pairs (GeoJSON format, ring should be closed)
   * @returns Area in hectares rounded to 2 decimal places, or 0 for degenerate polygons
   */
  calculateHectares(coordinates: [number, number][]): number {
    // Need at least 3 distinct vertices to form a polygon
    const distinctVertices = this.getDistinctVertices(coordinates);
    if (distinctVertices.length < 3) {
      return 0;
    }

    const areaSquareMeters = this.calculateSphericalArea(distinctVertices);
    const hectares = areaSquareMeters / 10000;
    return Math.round(hectares * 100) / 100;
  }

  /**
   * Extracts distinct vertices from the coordinate ring.
   * Removes the closing vertex if it duplicates the first, and removes consecutive duplicates.
   */
  private getDistinctVertices(coordinates: [number, number][]): [number, number][] {
    if (coordinates.length === 0) return [];

    const result: [number, number][] = [coordinates[0]];

    for (let i = 1; i < coordinates.length; i++) {
      const prev = result[result.length - 1];
      const curr = coordinates[i];
      if (curr[0] !== prev[0] || curr[1] !== prev[1]) {
        result.push(curr);
      }
    }

    // Remove closing vertex if it matches the first
    if (result.length > 1) {
      const first = result[0];
      const last = result[result.length - 1];
      if (first[0] === last[0] && first[1] === last[1]) {
        result.pop();
      }
    }

    return result;
  }

  /**
   * Calculates the spherical polygon area using the spherical excess formula.
   * Uses the shoelface-like formula on the unit sphere.
   *
   * @param vertices - Array of distinct [longitude, latitude] vertices (degrees)
   * @returns Area in square meters (always non-negative)
   */
  private calculateSphericalArea(vertices: [number, number][]): number {
    const n = vertices.length;
    if (n < 3) return 0;

    let sum = 0;

    for (let i = 0; i < n; i++) {
      const j = (i + 1) % n;
      const k = (i + 2) % n;

      const lon1 = this.toRadians(vertices[i][0]);
      const lat2 = this.toRadians(vertices[j][1]);
      const lon3 = this.toRadians(vertices[k][0]);

      sum += (lon3 - lon1) * Math.sin(lat2);
    }

    // The absolute area on the unit sphere, scaled to Earth's surface
    const areaOnSphere = Math.abs(sum) / 2;
    return areaOnSphere * AreaCalculationService.EARTH_RADIUS_M * AreaCalculationService.EARTH_RADIUS_M;
  }

  private toRadians(degrees: number): number {
    return (degrees * Math.PI) / 180;
  }
}
