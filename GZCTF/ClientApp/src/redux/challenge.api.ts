import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

export interface ChallengesDto {
  title: string;
  content: string;
  clientJS: string;
  answer: string;
  accessLevel: number;
  solvedCount: number;
  originalScore: number;
  minScore: number;
  expectMaxCount: number;
  awardCount: number;
  upgradeAccessLevel: number;
}

export interface ChallengesList {
  solved: number[];
  accessible: {
    id: number;
    title: string;
    acceptedCount: number;
    submissionCount: number;
    score: number;
  }[];
}

export interface AnswerChallengesDto {
  answer: string;
}

export const Challenges_API = createApi({
  reducerPath: 'ChallengesApi',
  baseQuery: fetchBaseQuery({ baseUrl: 'api/Challenges' }),
  refetchOnFocus: true,
  refetchOnReconnect: true,
  refetchOnMountOrArgChange: true,
  endpoints: (builder) => ({
    createChallenges: builder.query<void, ChallengesDto>({
      query: () => ({
        url: 'new',
        method: 'POST'
      })
    }),
    getChallenges: builder.query<ChallengesDto, number>({
      query: (id) => `${id}`
    }),
    updateChallenges: builder.query<void, [ChallengesDto, number]>({
      query: ([dto, id]) => ({
        url: `${id}`,
        method: 'PUT',
        body: dto
      })
    }),
    deleteChallenges: builder.query<void, number>({
      query: (id) => ({
        url: `${id}`,
        method: 'DELETE'
      })
    }),
    getChallengesList: builder.query<ChallengesList, void>({
      query: () => 'list'
    }),
    answerChallenges: builder.mutation<void, [AnswerChallengesDto, number]>({
      query: ([dto, id]) => ({
        url: `submit/${id}`,
        method: 'POST',
        body: dto
      })
    })
  })
});
