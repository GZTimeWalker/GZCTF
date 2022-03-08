import { createApi, fetchBaseQuery } from '@reduxjs/toolkit/query/react';

export interface PuzzleDto {
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

export interface PuzzleList {
  solved: number[];
  accessible: {
    id: number;
    title: string;
    acceptedCount: number;
    submissionCount: number;
    score: number;
  }[];
}

export interface AnswerPuzzleDto {
  answer: string;
}

export const PUZZLE_API = createApi({
  reducerPath: 'puzzleApi',
  baseQuery: fetchBaseQuery({ baseUrl: 'api/puzzle' }),
  refetchOnFocus: true,
  refetchOnReconnect: true,
  refetchOnMountOrArgChange: true,
  endpoints: (builder) => ({
    createPuzzle: builder.query<void, PuzzleDto>({
      query: () => ({
        url: 'new',
        method: 'POST'
      })
    }),
    getPuzzle: builder.query<PuzzleDto, number>({
      query: (id) => `${id}`
    }),
    updatePuzzle: builder.query<void, [PuzzleDto, number]>({
      query: ([dto, id]) => ({
        url: `${id}`,
        method: 'PUT',
        body: dto
      })
    }),
    deletePuzzle: builder.query<void, number>({
      query: (id) => ({
        url: `${id}`,
        method: 'DELETE'
      })
    }),
    getPuzzleList: builder.query<PuzzleList, void>({
      query: () => 'list'
    }),
    answerPuzzle: builder.mutation<void, [AnswerPuzzleDto, number]>({
      query: ([dto, id]) => ({
        url: `submit/${id}`,
        method: 'POST',
        body: dto
      })
    })
  })
});
