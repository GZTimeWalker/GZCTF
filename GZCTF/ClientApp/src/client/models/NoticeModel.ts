/* istanbul ignore file */
/* tslint:disable */
/* eslint-disable */

export type NoticeModel = {
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
    isPinned?: boolean;
};
