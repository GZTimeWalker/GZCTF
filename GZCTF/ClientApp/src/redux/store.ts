import { configureStore } from '@reduxjs/toolkit';
import { setupListeners } from '@reduxjs/toolkit/dist/query';
import { ADMIN_API } from './admin.api';
import { INFO_API } from './info.api';
import { Challenges_API } from './challenge.api';
import { SUBMISSION_API } from './submission.api';
import { USER_API } from './user.api';

export const store = configureStore({
  reducer: {
    [USER_API.reducerPath]: USER_API.reducer,
    [Challenges_API.reducerPath]: Challenges_API.reducer,
    [ADMIN_API.reducerPath]: ADMIN_API.reducer,
    [INFO_API.reducerPath]: INFO_API.reducer,
    [SUBMISSION_API.reducerPath]: SUBMISSION_API.reducer
  }/*,
  middleware: (getDefaultMiddleware) =>
    [USER_API, Challenges_API, ADMIN_API, INFO_API, SUBMISSION_API]
      .map((api) => api.middleware)
      .reduce((prev, curr) => prev.concat(curr), getDefaultMiddleware())*/
});

setupListeners(store.dispatch);
