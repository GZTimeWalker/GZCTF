import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';
import { Announcement } from './info.api';
export interface PuzzleLog {
  time: string;
  name: string;
  ip: string;
  msg: string;
  status: string;
}

export interface GetLogsParams {
  skip: number;
  count: number;
}

export interface UpdateAnnouncement {
  title: string;
  content: string;
  isPinned: boolean;
}

export const ADMIN_API = createApi({
  reducerPath: 'adminApi',
  baseQuery: fetchBaseQuery({ baseUrl: 'api/admin' }),
  endpoints: (builder) => ({
    getLogs: builder.query<PuzzleLog[], GetLogsParams>({
      query: (params) => ({
        url: 'logs',
        params
      })
    }),
    updateAnnouncements: builder.query<Announcement, UpdateAnnouncement>({
      query: (dto) => ({
        url: 'publish',
        method: 'POST',
        body: dto
      })
    })
  })
});
