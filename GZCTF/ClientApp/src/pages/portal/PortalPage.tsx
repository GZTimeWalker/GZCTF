import React, { FC } from 'react';
import { LoadingMask } from 'src/common/components/LoadingMask';
import {
  Container,
  Heading,
  VStack,
  Wrap,
  WrapItem,
  Text,
  Flex
} from '@chakra-ui/react';
import { INFO_API, Announcement } from '../../redux/info.api';
import marked from 'marked';
import '../../common/utils/marked.css';

function formatTime(time: string) {
  const date = new Date(time);
  return `${date.getMonth() + 1}/${date.getDate()} ${date.getHours()}:${date.getMinutes()}`;
}

const AnnouncementCard: FC<Announcement> = ({ time, isPinned, content, title }) => (
  <Flex justifyContent="space-between" alignItems="flex-end">
    <VStack px="18px" py="12px" align="sketch" spacing="0">
      <Heading color="gray.300" size="xl" textShadow="xl" pb="15px">
        # {title}
      </Heading>
      <div className="marked"
        dangerouslySetInnerHTML={{ __html: marked(content) }}
      />
      <Text pt="15px" fontFamily="mono" textAlign="right" color="gray.400">
        {formatTime(time)}
      </Text>
    </VStack>
  </Flex>
);

export const PortalPage: FC = () => {
  const { isLoading, error, data } = INFO_API.useGetAnnouncementsQuery();

  if (error || isLoading) {
    return <LoadingMask error={error} />;
  }

  return (
    <Container minHeight="100vh" maxWidth="70vw">
      <Wrap spacing="45px" justify="center" py="2em">
        {data?.map((a) => (
          <WrapItem key={a.title + a.time}>
            <AnnouncementCard {...a} />
          </WrapItem>
        ))}
      </Wrap>
    </Container>
  );
};
