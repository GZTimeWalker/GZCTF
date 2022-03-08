import { Box } from '@chakra-ui/react';
import React, { FC } from 'react';
import { Redirect, Route, RouteComponentProps, RouteProps, useLocation } from 'react-router-dom';
import { USER_API } from '../../redux/user.api';
import { LoadingMask } from '../components/LoadingMask';

export const AuthRoute: FC<RouteProps<string>> = ({ children, ...rest }) => {
  const { isLoading, error } = USER_API.useStatusQuery();
  const location = useLocation();

  const render = (props: RouteComponentProps) => {
    if (isLoading) {
      return (
        <Box h="100vh" w="100vw">
          <LoadingMask />
        </Box>
      );
    }

    if (error) {
      return (
        <Redirect
          to={{
            pathname: '/login',
            state: { from: location },
            search: 'redirect=' + encodeURIComponent(`${location.pathname}`)
          }}
        />
      );
    }

    if (rest.render) {
      return rest.render(props);
    } else {
      return children;
    }
  };

  return <Route {...rest} render={render} />;
};
