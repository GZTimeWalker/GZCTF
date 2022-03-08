import {
  Box,
  Flex,
  IconButton,
  Popover,
  PopoverArrow,
  PopoverCloseButton,
  PopoverContent,
  PopoverTrigger,
  Spacer,
  VStack
} from '@chakra-ui/react';
import React, { FC } from 'react';
import { LogoIcon } from './LogoIcon';
import { RouterButton } from './RouterButton';
import { HomeIcon } from './HomeIcon';
import { RoadMapIcon } from './RoadMapIcon';
import { UserIcon } from './UserIcon';
import { LeaderBoardIcon } from './LeaderBoardIcon';
import { UserStatus } from './UserStatus';

export const NavBar: FC = () => {
  return (
    <Flex h="100%" px="12px" py="18px" bg="gray.700" flexDirection="column" alignItems="center">
      <Box h="48px" w="48px" flex="none">
        <LogoIcon />
      </Box>
      <VStack my="24px" spacing="24px">
        <RouterButton
          route="/"
          matchExactly={true}
          icon={<HomeIcon width="24px" height="24px" />}
        />
        <RouterButton route="/puzzle" icon={<RoadMapIcon width="24px" height="24px" />} />
        <RouterButton route="/leaderboard" icon={<LeaderBoardIcon width="24px" height="24px" />} />
      </VStack>
      <Spacer />
      <Popover placement="right">
        <PopoverTrigger>
          <IconButton
            aria-label=""
            variant="ghost"
            colorScheme="gray"
            icon={<UserIcon width="24px" height="24px" />}
            size="lg"
            rounded="xl"
          />
        </PopoverTrigger>
        <PopoverContent m="12px" ml="0">
          <PopoverArrow />
          <PopoverCloseButton />
          <UserStatus />
        </PopoverContent>
      </Popover>
    </Flex>
  );
};
