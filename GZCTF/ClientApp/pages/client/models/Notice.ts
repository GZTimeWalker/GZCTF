/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

export type Notice = {
    id?: number;
    /**
     * 通知标题
     */
    title: string;
    /**
     * 通知内容
     */
    content: string;
    /**
     * 是否置顶
     */
    isPinned: boolean;
    /**
     * 发布时间
     */
    time: string;
};
