/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

export type BasicGameInfoModel = {
    id?: number;
    /**
     * 比赛标题
     */
    title?: string;
    /**
     * 比赛描述
     */
    summary?: string;
    /**
     * 队员数量限制
     */
    limit?: number;
    /**
     * 开始时间
     */
    start?: string;
    /**
     * 结束时间
     */
    end?: string;
};
