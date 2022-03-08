import { IconButton } from '@chakra-ui/react';
import React, { FC, ReactElement, useMemo } from 'react';
import { useLocation } from 'react-router';
import { Link } from 'react-router-dom';

export interface RouterButtonProps {
  icon: ReactElement;
  route: string;
  matchExactly?: boolean;
}

export const RouterButton: FC<RouterButtonProps> = ({ icon, route, matchExactly }) => {
  const location = useLocation();
  const active = useMemo(() => {
    if (!matchExactly) {
      return location.pathname.startsWith(route);
    } else {
      return location.pathname.toString() === route;
    }
  }, [location, route, matchExactly]);

  return (
    <Link to={route}>
      <IconButton
        aria-label=""
        colorScheme="gray"
        variant={active ? 'outline' : 'ghost'}
        icon={icon}
        size="lg"
        rounded="xl"
      />
    </Link>
  );
};
