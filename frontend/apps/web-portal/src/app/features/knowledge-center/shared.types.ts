/**
 * Generic paged-result envelope used across web-portal feature areas.
 * Mirrors the backend's CCE.Application.Common.Pagination.PagedResult<T>.
 */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}
