export interface InteractiveMap {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
  nodes: InteractiveMapNode[];
}

export interface InteractiveMapNode {
  id: string;
  nameAr: string;
  nameEn: string;
  iconKey: string;
  level: number;
  parentId?: string | null;
  category?: number | null;
  categoryNameAr?: string | null;
  categoryNameEn?: string | null;
  topicId: string;
  tags: string[];
}

export const NODE_LEVELS = [0, 1, 2] as const;
export type NodeLevel = 0 | 1 | 2;

// ─── Node detail drawer (GET /api/interactive-maps/nodes/{id}/details) ───

export interface NodeDetailTopic {
  id: string;
  nameAr: string;
  nameEn: string;
  descriptionAr: string;
  descriptionEn: string;
  slug: string;
}

export interface NodeDetailResource {
  id: string;
  titleAr: string;
  titleEn: string;
  resourceType: string;
  categoryNameAr?: string | null;
  categoryNameEn?: string | null;
  publishedOn: string;
}

export interface NodeDetailNews {
  id: string;
  titleAr: string;
  titleEn: string;
  featuredImageUrl?: string | null;
  publishedOn: string;
}

export interface NodeDetailEvent {
  id: string;
  titleAr: string;
  titleEn: string;
  startsOn: string;
  endsOn: string;
  featuredImageUrl?: string | null;
}

export interface NodeDetailPost {
  id: string;
  type: 'info' | 'poll' | 'question' | string;
  title: string;
  content: string;
  commentsCount: number;
  createdOn: string;
}

export interface NodeDetails {
  node: {
    id: string;
    nameAr: string;
    nameEn: string;
    iconKey: string;
    topicId: string;
    titleAr?: string | null;
    titleEn?: string | null;
    descriptionAr?: string | null;
    descriptionEn?: string | null;
  };
  topic?: NodeDetailTopic | null;
  resources: NodeDetailResource[];
  news: NodeDetailNews[];
  events: NodeDetailEvent[];
  posts?: NodeDetailPost[] | null;
}
