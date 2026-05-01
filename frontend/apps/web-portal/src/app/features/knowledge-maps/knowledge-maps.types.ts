export interface KnowledgeMap {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
  slug: string;
  isActive: boolean;
}

export type NodeType = 'Technology' | 'Sector' | 'SubTopic';
export const NODE_TYPES: readonly NodeType[] = ['Technology', 'Sector', 'SubTopic'] as const;

export type RelationshipType = 'ParentOf' | 'RelatedTo' | 'RequiredBy';
export const RELATIONSHIP_TYPES: readonly RelationshipType[] = [
  'ParentOf',
  'RelatedTo',
  'RequiredBy',
] as const;

export interface KnowledgeMapNode {
  id: string;
  mapId: string;
  nameAr: string;
  nameEn: string;
  nodeType: NodeType;
  descriptionAr: string | null;
  descriptionEn: string | null;
  iconUrl: string | null;
  layoutX: number;
  layoutY: number;
  orderIndex: number;
}

export interface KnowledgeMapEdge {
  id: string;
  mapId: string;
  fromNodeId: string;
  toNodeId: string;
  relationshipType: RelationshipType;
  orderIndex: number;
}
