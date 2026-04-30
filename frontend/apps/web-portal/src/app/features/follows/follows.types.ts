export interface MyFollows {
  topicIds: string[];
  userIds: string[];
  postIds: string[];
}

export type FollowEntityType = 'topic' | 'user' | 'post';

/** Maps a singular entity type to the URL plural segment used by /api/me/follows. */
export const FOLLOW_PATH_SEGMENT: Record<FollowEntityType, string> = {
  topic: 'topics',
  user: 'users',
  post: 'posts',
};
