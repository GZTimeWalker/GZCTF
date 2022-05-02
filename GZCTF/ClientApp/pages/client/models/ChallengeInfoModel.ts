/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { ChallengeTag } from './ChallengeTag';
import type { ChallengeType } from './ChallengeType';

export type ChallengeInfoModel = {
    /**
     * 题目Id
     */
    id?: number;
    /**
     * 题目名称
     */
    title: string;
    /**
     * 题目标签
     */
    tag?: ChallengeTag;
    /**
     * 题目类型
     */
    type?: ChallengeType;
};
