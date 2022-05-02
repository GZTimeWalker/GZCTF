/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { SubmissionType } from './SubmissionType';

export type ChallengeItem = {
    /**
     * 题目 Id
     */
    id?: number;
    /**
     * 题目分值
     */
    score?: number;
    /**
     * 未解出、一血、二血、三血或者其他
     */
    rank?: SubmissionType;
};
