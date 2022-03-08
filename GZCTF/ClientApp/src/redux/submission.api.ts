import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

export interface Submission {
  puzzleId: number;
  answer: string;
  solved: boolean;
  score: number;
  time: string;
  userName: string;
}

export const SUBMISSION_API = createApi({
  reducerPath: 'submissionApi',
  baseQuery: fetchBaseQuery({ baseUrl: 'api/submission' }),
  refetchOnFocus: true,
  refetchOnReconnect: true,
  refetchOnMountOrArgChange: true,
  endpoints: (builder) => ({
    getLatestSubmissions: builder.query<Submission[], number>({
      query: (id) => `${id}`
    }),
    getLatestSubmissionsOfAllUsers: builder.query<Submission[], number>({
      query: (id) => `history/${id}`
    })
  })
});
