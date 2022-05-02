/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { ChallengeTag } from './ChallengeTag';

export type ChallengeInfo = {
    /**
     * 题目Id
     */
    id?: number;
    /**
     * 题目名称
     */
    title?: string;
    /**
     * 题目标签
     */
    tag?: ChallengeTag;
    /**
     * 题目分值
     */
    score?: number;
};
