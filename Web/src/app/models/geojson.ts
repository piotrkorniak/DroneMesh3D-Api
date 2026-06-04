// models/geojson.ts
export interface GeoJsonPolygon {
  type: 'Polygon';
  coordinates: number[][][]; // [ring[vertex[lng, lat]]]
}
