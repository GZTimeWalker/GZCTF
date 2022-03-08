import { Box } from '@chakra-ui/react';
import React, { FC } from 'react';
import { Redirect, Route, Switch } from 'react-router';
import { AuthRoute } from './common/routes/AuthRoute';
import { LoginPage } from './pages/login/LoginPage';
import { NotFoundPage } from './pages/not-found/NotFoundPage';
import { WithNavBar } from './common/components/WithNavBar';
import { PortalPage } from './pages/portal/PortalPage';
import { PuzzlePage } from './pages/puzzle/PuzzlePage';
import { LeaderBoardPage } from './pages/leaderboard/LeaderBoardPage';
import { PuzzleDetailPage } from './pages/puzzle-detail/PuzzleDetailPage';
import { ResetPasswordPage } from './pages/reset/ResetPasswordPage';
import { VerifyEmailPage } from './pages/verify/VerifyEmailPage';
import { AccountManagePage } from './pages/account-manage/AccountManagePage';
import { ChangeInfoPage } from './pages/change-info/ChangeInfoPage';
import { LogsPage } from './pages/logs/LogsPage';
import { EmailConfirmPage } from './pages/email-confirm/EmailConfirmPage';

export const App: FC = () => {
  return (
    <Box minHeight="100vh" width="100vw">
      <Switch>
        <AuthRoute exact path="/">
          <WithNavBar>
            <PortalPage />
          </WithNavBar>
        </AuthRoute>
        <Route path="/reset">
          <ResetPasswordPage />
        </Route>
        <Route path="/verify">
          <VerifyEmailPage />
        </Route>
        <Route path="/confirm">
          <EmailConfirmPage />
        </Route>
        <AuthRoute path="/account-manage">
          <WithNavBar>
            <AccountManagePage />
          </WithNavBar>
        </AuthRoute>
        <AuthRoute path="/change-info">
          <WithNavBar>
            <ChangeInfoPage />
          </WithNavBar>
        </AuthRoute>
        <AuthRoute path="/logs">
          <WithNavBar>
            <LogsPage />
          </WithNavBar>
        </AuthRoute>
        <Route path="/login">
          <LoginPage />
        </Route>
        <AuthRoute
          path="/puzzle/:id"
          render={({ match }) => (
            <WithNavBar>
              <PuzzleDetailPage id={Number(match.params.id)} />
            </WithNavBar>
          )}
        />
        <AuthRoute path="/puzzle">
          <WithNavBar>
            <PuzzlePage />
          </WithNavBar>
        </AuthRoute>
        <AuthRoute path="/leaderboard">
          <WithNavBar>
            <LeaderBoardPage />
          </WithNavBar>
        </AuthRoute>
        <Route path="/404">
          <NotFoundPage />
        </Route>
        <Redirect to="/404" />
      </Switch>
    </Box>
  );
};
