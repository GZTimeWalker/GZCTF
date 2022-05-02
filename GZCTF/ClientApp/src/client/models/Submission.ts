/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

import type { AnswerResult } from './AnswerResult';

export type Submission = {
    /**
     * 提交的答案字符串
     */
    answer?: string;
    /**
     * 提交的答案状态
     */
    status?: AnswerResult;
    /**
     * 答案提交的时间
     */
    time?: string;
};
