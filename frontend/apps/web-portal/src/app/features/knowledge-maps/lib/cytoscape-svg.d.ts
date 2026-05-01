/**
 * Ambient declaration for cytoscape-svg. Upstream package ships no
 * type definitions and no @types/cytoscape-svg exists. We treat the
 * default export as an opaque plugin object — `cy.use()` accepts it
 * via `as never` in the loader.
 */
declare module 'cytoscape-svg' {
  const plugin: unknown;
  export default plugin;
}
